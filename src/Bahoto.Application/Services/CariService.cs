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

    public async Task<ApiResponse<PagedResult<CariProductGroupDto>>> ListAsync(ListCariRequest request, CancellationToken cancellationToken = default)
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

        var productsWithAnyCari = _db.Caris.AsNoTracking().Select(c => c.ProductId).Distinct();
        var productsWithCariInRange = cariQuery.Select(c => c.ProductId).Distinct();

        // Cari hareketi olan ürünler + henüz hareketi olmayan ürünler
        // (yalnızca marka ile oluşturulan kayıtların listede görünmesi için).
        var productsQuery = _db.Products.AsNoTracking().Where(p =>
            productsWithCariInRange.Contains(p.Id) ||
            !productsWithAnyCari.Contains(p.Id));

        var productSummaries = productsQuery.Select(p => new
        {
            ProductId = p.Id,
            Brand = p.Brand,
            Name = p.Name,
            LastAt = cariQuery
                .Where(c => c.ProductId == p.Id)
                .Select(c => (DateTime?)c.CreatedAt)
                .Max() ?? p.CreatedAt,
            IncomingAmount = cariQuery
                .Where(c => c.ProductId == p.Id)
                .Sum(c => (decimal?)c.IncomingAmount) ?? 0m,
            PaidAmount = cariQuery
                .Where(c => c.ProductId == p.Id)
                .Sum(c => (decimal?)c.PaidAmount) ?? 0m
        });

        var totalCount = await productSummaries.CountAsync(cancellationToken);

        var pageItems = await productSummaries
            .OrderByDescending(x => x.LastAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var productIds = pageItems.Select(x => x.ProductId).ToList();

        var entries = await cariQuery
            .Include(x => x.Product)
            .Where(c => productIds.Contains(c.ProductId))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var entriesByProduct = entries
            .GroupBy(c => c.ProductId)
            .ToDictionary(g => g.Key, g => g.Select(Map).ToList());

        var items = pageItems
            .Select(x =>
            {
                var groupEntries = entriesByProduct.GetValueOrDefault(x.ProductId) ?? [];
                return new CariProductGroupDto
                {
                    ProductId = x.ProductId,
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
            .Include(x => x.Product)
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

        var product = await _db.Products.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);
        if (product is null)
        {
            return ApiResponse<CariDto>.Fail("Seçilen ürün bulunamadı.");
        }

        var unit = NormalizeUnit(request.QuantityUnit);
        var entity = new Cari
        {
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            QuantityUnit = unit,
            IncomingAmount = request.IncomingAmount,
            PaidAmount = request.PaidAmount,
            Balance = request.IncomingAmount - request.PaidAmount
        };

        _db.Caris.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        entity.Product = product;
        return ApiResponse<CariDto>.Ok(Map(entity), "Cari kaydı oluşturuldu.");
    }

    public async Task<ApiResponse<CariProductGroupDto>> CreateWithProductAsync(
        CreateCariWithProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var brand = request.Brand?.Trim() ?? string.Empty;
        var name = request.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(brand))
        {
            return ApiResponse<CariProductGroupDto>.Fail("Marka zorunludur.");
        }

        var hasEntry = request.Quantity.HasValue
            || request.IncomingAmount.HasValue
            || request.PaidAmount.HasValue;

        if (hasEntry)
        {
            if (!request.Quantity.HasValue
                || !request.IncomingAmount.HasValue
                || !request.PaidAmount.HasValue)
            {
                return ApiResponse<CariProductGroupDto>.Fail(
                    "Hareket eklemek için miktar, gelen fiyat ve ödenen fiyat alanlarının hepsini doldurun.");
            }

            var validationError = ValidateRequest(
                request.Quantity.Value,
                request.QuantityUnit,
                request.IncomingAmount.Value,
                request.PaidAmount.Value);
            if (validationError is not null)
            {
                return ApiResponse<CariProductGroupDto>.Fail(validationError);
            }
        }

        var brandLower = brand.ToLowerInvariant();
        var nameLower = name.ToLowerInvariant();
        var exists = await _db.Products.AnyAsync(
            p => p.Brand.ToLower() == brandLower && p.Name.ToLower() == nameLower,
            cancellationToken);

        if (exists)
        {
            return ApiResponse<CariProductGroupDto>.Fail(
                string.IsNullOrEmpty(name)
                    ? "Bu marka için ürün adı boş bir kayıt zaten var. Listedeki satırı açın veya ürün adı girin."
                    : "Bu marka ve ürün adı zaten kayıtlı. Aynı ürüne ekleme için listedeki satırı açın.");
        }

        var product = new Product
        {
            Brand = brand,
            Name = name
        };

        _db.Products.Add(product);

        List<CariDto> entries = [];
        decimal incomingAmount = 0;
        decimal paidAmount = 0;

        if (hasEntry)
        {
            var unit = NormalizeUnit(request.QuantityUnit);
            var cari = new Cari
            {
                Product = product,
                Quantity = request.Quantity!.Value,
                QuantityUnit = unit,
                IncomingAmount = request.IncomingAmount!.Value,
                PaidAmount = request.PaidAmount!.Value,
                Balance = request.IncomingAmount.Value - request.PaidAmount.Value
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
            ProductId = product.Id,
            Brand = product.Brand,
            Name = product.Name,
            QuantityLabel = FormatQuantityLabel(entries),
            IncomingAmount = incomingAmount,
            PaidAmount = paidAmount,
            Balance = incomingAmount - paidAmount,
            Entries = entries
        }, hasEntry ? "Cari kaydı oluşturuldu." : "Ürün oluşturuldu.");
    }

    public async Task<ApiResponse<CariDto>> UpdateAsync(UpdateCariRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Caris
            .Include(x => x.Product)
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

        var product = await _db.Products.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);
        if (product is null)
        {
            return ApiResponse<CariDto>.Fail("Seçilen ürün bulunamadı.");
        }

        entity.ProductId = request.ProductId;
        entity.Quantity = request.Quantity;
        entity.QuantityUnit = NormalizeUnit(request.QuantityUnit);
        entity.IncomingAmount = request.IncomingAmount;
        entity.PaidAmount = request.PaidAmount;
        entity.Balance = request.IncomingAmount - request.PaidAmount;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        entity.Product = product;
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
        ProductId = entity.ProductId,
        ProductBrand = entity.Product?.Brand ?? string.Empty,
        ProductName = entity.Product?.Name ?? string.Empty,
        Quantity = entity.Quantity,
        QuantityUnit = string.IsNullOrWhiteSpace(entity.QuantityUnit) ? "Adet" : entity.QuantityUnit,
        IncomingAmount = entity.IncomingAmount,
        PaidAmount = entity.PaidAmount,
        Balance = entity.Balance,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
