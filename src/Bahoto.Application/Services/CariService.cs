using Bahoto.Application.Common;
using Bahoto.Application.DTOs.Cari;
using Bahoto.Application.DTOs.OilChange;
using Bahoto.Application.Interfaces;
using Bahoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bahoto.Application.Services;

public class CariService : ICariService
{
    private static readonly HashSet<string> AllowedUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "Adet",
        "Koli"
    };

    private readonly IApplicationDbContext _db;

    public CariService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<PagedResult<CariProductGroupDto>>> ListAsync(
        ListCariRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 1000);

        var cariQuery = _db.Caris.AsNoTracking().AsQueryable();

        if (request.StartDate is DateOnly startDate)
        {
            var startUtc = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            cariQuery = cariQuery.Where(c => c.CreatedAt >= startUtc);
        }

        if (request.EndDate is DateOnly endDate)
        {
            var endExclusiveUtc = DateTime.SpecifyKind(
                endDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc);
            cariQuery = cariQuery.Where(c => c.CreatedAt < endExclusiveUtc);
        }

        var itemsWithAnyCari = _db.Caris.AsNoTracking().Select(c => c.CariItemId).Distinct();
        var itemsWithCariInRange = cariQuery.Select(c => c.CariItemId).Distinct();

        var itemsQuery = _db.CariItems.AsNoTracking().Where(p =>
            itemsWithCariInRange.Contains(p.Id) ||
            !itemsWithAnyCari.Contains(p.Id));

        var summaries = itemsQuery.Select(p => new
        {
            CariItemId = p.Id,
            Brand = p.Brand,
            Name = p.Name,
            LastAt = cariQuery
                .Where(c => c.CariItemId == p.Id)
                .Select(c => (DateTime?)c.CreatedAt)
                .Max() ?? p.CreatedAt,
            IncomingAmount = cariQuery
                .Where(c => c.CariItemId == p.Id)
                .Sum(c => (decimal?)c.IncomingAmount) ?? 0m,
            PaidAmount = cariQuery
                .Where(c => c.CariItemId == p.Id)
                .Sum(c => (decimal?)c.PaidAmount) ?? 0m
        });

        var totalCount = await summaries.CountAsync(cancellationToken);

        var pageItems = await summaries
            .OrderByDescending(x => x.LastAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var itemIds = pageItems.Select(x => x.CariItemId).ToList();

        var entries = await cariQuery
            .Include(x => x.CariItem)
            .Where(c => itemIds.Contains(c.CariItemId))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var entriesByItem = entries
            .GroupBy(c => c.CariItemId)
            .ToDictionary(g => g.Key, g => g.Select(Map).ToList());

        var items = pageItems
            .Select(x =>
            {
                var groupEntries = entriesByItem.GetValueOrDefault(x.CariItemId) ?? [];
                return new CariProductGroupDto
                {
                    CariItemId = x.CariItemId,
                    Brand = x.Brand,
                    Name = x.Name,
                    QuantityLabel = FormatQuantityLabel(groupEntries),
                    IncomingAmount = x.IncomingAmount,
                    PaidAmount = x.PaidAmount,
                    Balance = x.IncomingAmount - x.PaidAmount,
                    Entries = groupEntries
                };
            })
            .ToList();

        return ApiResponse<PagedResult<CariProductGroupDto>>.Ok(new PagedResult<CariProductGroupDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    public async Task<ApiResponse<CariDto>> GetAsync(GetCariRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Caris.AsNoTracking()
            .Include(x => x.CariItem)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return ApiResponse<CariDto>.Fail("Cari kaydı bulunamadı.");
        }

        return ApiResponse<CariDto>.Ok(Map(entity));
    }

    public async Task<ApiResponse<CariDto>> CreateAsync(CreateCariRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRequest(request.Quantity, request.QuantityUnit, request.IncomingAmount, request.PaidAmount);
        if (validationError is not null)
        {
            return ApiResponse<CariDto>.Fail(validationError);
        }

        var item = await _db.CariItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.CariItemId, cancellationToken);
        if (item is null)
        {
            return ApiResponse<CariDto>.Fail("Seçilen cari ürünü bulunamadı.");
        }

        var unit = NormalizeUnit(request.QuantityUnit);
        var entity = new Cari
        {
            CariItemId = request.CariItemId,
            Quantity = request.Quantity,
            QuantityUnit = unit,
            IncomingAmount = request.IncomingAmount,
            PaidAmount = request.PaidAmount,
            Balance = request.IncomingAmount - request.PaidAmount
        };

        _db.Caris.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        entity.CariItem = item;
        return ApiResponse<CariDto>.Ok(Map(entity), "Cari kaydı oluşturuldu.");
    }

    public async Task<ApiResponse<CariProductGroupDto>> CreateWithProductAsync(
        CreateCariWithProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var brand = request.Brand?.Trim() ?? string.Empty;
        var name = request.Name?.Trim() ?? string.Empty;

        var hasEntry = request.Quantity.HasValue
            || request.IncomingAmount.HasValue
            || request.PaidAmount.HasValue;

        int? quantity = request.Quantity;
        decimal? incomingAmountValue = request.IncomingAmount;
        decimal? paidAmountValue = request.PaidAmount;

        if (hasEntry)
        {
            if (!incomingAmountValue.HasValue || !paidAmountValue.HasValue)
            {
                return ApiResponse<CariProductGroupDto>.Fail(
                    "Hareket eklemek için gelen fiyat ve ödenen fiyat alanlarını doldurun.");
            }

            quantity ??= 1;

            var validationError = ValidateRequest(
                quantity.Value,
                request.QuantityUnit,
                incomingAmountValue.Value,
                paidAmountValue.Value);
            if (validationError is not null)
            {
                return ApiResponse<CariProductGroupDto>.Fail(validationError);
            }
        }

        var brandLower = brand.ToLowerInvariant();
        var nameLower = name.ToLowerInvariant();
        var exists = await _db.CariItems.AnyAsync(
            p => p.Brand.ToLower() == brandLower && p.Name.ToLower() == nameLower,
            cancellationToken);

        if (exists)
        {
            return ApiResponse<CariProductGroupDto>.Fail(
                string.IsNullOrEmpty(brand) && string.IsNullOrEmpty(name)
                    ? "Bu kayıt zaten var."
                    : string.IsNullOrEmpty(brand)
                        ? "Bu ürün adı zaten kayıtlı. Aynı ürüne ekleme için listedeki satırı açın."
                        : string.IsNullOrEmpty(name)
                            ? "Bu marka için ürün adı boş bir kayıt zaten var. Listedeki satırı açın veya ürün adı girin."
                            : "Bu marka ve ürün adı zaten kayıtlı. Aynı ürüne ekleme için listedeki satırı açın.");
        }

        var item = new CariItem
        {
            Brand = brand,
            Name = name
        };

        _db.CariItems.Add(item);

        List<CariDto> entries = [];
        decimal incomingAmount = 0;
        decimal paidAmount = 0;

        if (hasEntry)
        {
            var unit = NormalizeUnit(request.QuantityUnit);
            var cari = new Cari
            {
                CariItem = item,
                Quantity = quantity!.Value,
                QuantityUnit = unit,
                IncomingAmount = incomingAmountValue!.Value,
                PaidAmount = paidAmountValue!.Value,
                Balance = incomingAmountValue.Value - paidAmountValue.Value
            };

            _db.Caris.Add(cari);
            await _db.SaveChangesAsync(cancellationToken);

            var mapped = Map(cari);
            entries = [mapped];
            incomingAmount = cari.IncomingAmount;
            paidAmount = cari.PaidAmount;
        }
        else
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<CariProductGroupDto>.Ok(new CariProductGroupDto
        {
            CariItemId = item.Id,
            Brand = item.Brand,
            Name = item.Name,
            QuantityLabel = FormatQuantityLabel(entries),
            IncomingAmount = incomingAmount,
            PaidAmount = paidAmount,
            Balance = incomingAmount - paidAmount,
            Entries = entries
        }, hasEntry ? "Cari kaydı oluşturuldu." : "Cari ürünü oluşturuldu.");
    }

    public async Task<ApiResponse<CariDto>> UpdateAsync(UpdateCariRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Caris
            .Include(x => x.CariItem)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return ApiResponse<CariDto>.Fail("Cari kaydı bulunamadı.");
        }

        var validationError = ValidateRequest(request.Quantity, request.QuantityUnit, request.IncomingAmount, request.PaidAmount);
        if (validationError is not null)
        {
            return ApiResponse<CariDto>.Fail(validationError);
        }

        var item = await _db.CariItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.CariItemId, cancellationToken);
        if (item is null)
        {
            return ApiResponse<CariDto>.Fail("Seçilen cari ürünü bulunamadı.");
        }

        entity.CariItemId = request.CariItemId;
        entity.Quantity = request.Quantity;
        entity.QuantityUnit = NormalizeUnit(request.QuantityUnit);
        entity.IncomingAmount = request.IncomingAmount;
        entity.PaidAmount = request.PaidAmount;
        entity.Balance = request.IncomingAmount - request.PaidAmount;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        entity.CariItem = item;
        return ApiResponse<CariDto>.Ok(Map(entity), "Cari kaydı güncellendi.");
    }

    public async Task<ApiResponse> DeleteAsync(DeleteCariRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Caris.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return ApiResponse.Fail("Cari kaydı bulunamadı.");
        }

        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Cari kaydı silindi.");
    }

    public async Task<ApiResponse> DeleteItemAsync(
        DeleteCariItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await _db.CariItems.FirstOrDefaultAsync(x => x.Id == request.CariItemId, cancellationToken);
        if (item is null)
        {
            return ApiResponse.Fail("Cari ürünü bulunamadı.");
        }

        var utcNow = DateTime.UtcNow;
        item.DeletedAt = utcNow;
        item.UpdatedAt = utcNow;

        var entries = await _db.Caris
            .Where(x => x.CariItemId == item.Id)
            .ToListAsync(cancellationToken);

        foreach (var entry in entries)
        {
            entry.DeletedAt = utcNow;
            entry.UpdatedAt = utcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse.Ok("Cari ürünü ve hareketleri silindi.");
    }

    public async Task<ApiResponse> DeleteBrandAsync(
        DeleteCariBrandRequest request,
        CancellationToken cancellationToken = default)
    {
        var brand = request.Brand?.Trim() ?? string.Empty;
        var brandLower = brand.ToLowerInvariant();

        var items = await _db.CariItems
            .Where(x => x.Brand.ToLower() == brandLower)
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return ApiResponse.Fail("Marka bulunamadı.");
        }

        var utcNow = DateTime.UtcNow;
        var itemIds = items.Select(x => x.Id).ToList();

        foreach (var item in items)
        {
            item.DeletedAt = utcNow;
            item.UpdatedAt = utcNow;
        }

        var entries = await _db.Caris
            .Where(x => itemIds.Contains(x.CariItemId))
            .ToListAsync(cancellationToken);

        foreach (var entry in entries)
        {
            entry.DeletedAt = utcNow;
            entry.UpdatedAt = utcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse.Ok(
            string.IsNullOrEmpty(brand)
                ? "Markasız cari kayıtları silindi."
                : $"“{brand}” markası ve bağlı cari kayıtları silindi.");
    }

    private static string? ValidateRequest(int quantity, string? quantityUnit, decimal incomingAmount, decimal paidAmount)
    {
        if (quantity < 1)
        {
            return "Miktar en az 1 olmalıdır.";
        }

        if (string.IsNullOrWhiteSpace(quantityUnit) || !AllowedUnits.Contains(quantityUnit.Trim()))
        {
            return "Birim olarak Adet veya Koli seçilmelidir.";
        }

        if (incomingAmount < 0)
        {
            return "Gelen fiyat geçerli bir değer olmalıdır.";
        }

        if (paidAmount < 0)
        {
            return "Ödenen fiyat geçerli bir değer olmalıdır.";
        }

        return null;
    }

    private static string NormalizeUnit(string? unit)
    {
        var value = unit?.Trim() ?? "Adet";
        if (value.Equals("Koli", StringComparison.OrdinalIgnoreCase))
        {
            return "Koli";
        }

        return "Adet";
    }

    private static string FormatQuantityLabel(IReadOnlyList<CariDto> entries)
    {
        if (entries.Count == 0)
        {
            return "—";
        }

        var parts = entries
            .GroupBy(e => NormalizeUnit(e.QuantityUnit))
            .OrderBy(g => g.Key == "Koli" ? 0 : 1)
            .Select(g => $"{g.Sum(x => x.Quantity)} {g.Key.ToLowerInvariant()}")
            .ToList();

        return string.Join(", ", parts);
    }

    private static CariDto Map(Cari entity) => new()
    {
        Id = entity.Id,
        CariItemId = entity.CariItemId,
        ProductBrand = entity.CariItem?.Brand ?? string.Empty,
        ProductName = entity.CariItem?.Name ?? string.Empty,
        Quantity = entity.Quantity,
        QuantityUnit = string.IsNullOrWhiteSpace(entity.QuantityUnit) ? "Adet" : entity.QuantityUnit,
        IncomingAmount = entity.IncomingAmount,
        PaidAmount = entity.PaidAmount,
        Balance = entity.Balance,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
