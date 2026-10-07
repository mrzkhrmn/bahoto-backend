using Bahoto.Application.Common;
using Bahoto.Application.DTOs.DryLubePrice;
using Bahoto.Application.Interfaces;
using Bahoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bahoto.Application.Services;

public class DryLubePriceService : IDryLubePriceService
{
    private static readonly HashSet<string> ServiceKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "normal",
        "waterless",
        "underWashDryLube"
    };

    private readonly IApplicationDbContext _db;

    public DryLubePriceService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<DryLubePriceListData>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.DryLubePrices.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.BrandModel)
            .ToListAsync(cancellationToken);

        return ApiResponse<DryLubePriceListData>.Ok(new DryLubePriceListData
        {
            Items = items.Select(Map).ToList(),
            TableLastPriceDate = items.Count == 0 ? null : items.Max(x => x.LastPriceDate)
        });
    }

    public async Task<ApiResponse<DryLubePriceDto>> CreateAsync(CreateDryLubePriceRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.BrandModel))
        {
            return ApiResponse<DryLubePriceDto>.Fail("Marka-Model zorunludur.");
        }

        if (HasNegative(request))
        {
            return ApiResponse<DryLubePriceDto>.Fail("Fiyat negatif olamaz.");
        }

        var brandModel = request.BrandModel.Trim();
        var exists = await _db.DryLubePrices.AnyAsync(
            x => x.BrandModel.ToLower() == brandModel.ToLower(),
            cancellationToken);

        if (exists)
        {
            return ApiResponse<DryLubePriceDto>.Fail("Bu Marka-Model zaten kayıtlı.");
        }

        var maxSort = await _db.DryLubePrices.MaxAsync(x => (int?)x.SortOrder, cancellationToken) ?? -1;
        var entity = new DryLubePrice
        {
            BrandModel = brandModel,
            SortOrder = maxSort + 1
        };
        ApplyPrices(entity, request);

        if (HasAnyPrice(entity))
        {
            entity.LastPriceDate = DateTime.UtcNow;
        }

        _db.DryLubePrices.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<DryLubePriceDto>.Ok(Map(entity), "Kuru yağlama fiyatı eklendi.");
    }

    public async Task<ApiResponse<DryLubePriceDto>> UpdateAsync(UpdateDryLubePriceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.DryLubePrices.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return ApiResponse<DryLubePriceDto>.Fail("Kayıt bulunamadı.");
        }

        if (string.IsNullOrWhiteSpace(request.BrandModel))
        {
            return ApiResponse<DryLubePriceDto>.Fail("Marka-Model zorunludur.");
        }

        if (HasNegative(request))
        {
            return ApiResponse<DryLubePriceDto>.Fail("Fiyat negatif olamaz.");
        }

        var brandModel = request.BrandModel.Trim();
        var duplicate = await _db.DryLubePrices.AnyAsync(
            x => x.Id != request.Id && x.BrandModel.ToLower() == brandModel.ToLower(),
            cancellationToken);

        if (duplicate)
        {
            return ApiResponse<DryLubePriceDto>.Fail("Bu Marka-Model zaten kayıtlı.");
        }

        var before = Snapshot(entity);
        entity.BrandModel = brandModel;
        ApplyPrices(entity, request);

        if (PricesDiffer(before, Snapshot(entity)))
        {
            entity.LastPriceDate = DateTime.UtcNow;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<DryLubePriceDto>.Ok(Map(entity), "Kuru yağlama fiyatı güncellendi.");
    }

    public async Task<ApiResponse> DeleteAsync(DeleteDryLubePriceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.DryLubePrices.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return ApiResponse.Fail("Kayıt bulunamadı.");
        }

        var utcNow = DateTime.UtcNow;
        entity.DeletedAt = utcNow;
        entity.UpdatedAt = utcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Kuru yağlama fiyatı silindi.");
    }

    public async Task<ApiResponse> ReorderAsync(ReorderDryLubePriceRequest request, CancellationToken cancellationToken = default)
    {
        var ids = request.Ids.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count < 2)
        {
            return ApiResponse.Fail("Sıralama için en az iki kayıt gerekir.");
        }

        var entities = await _db.DryLubePrices
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (entities.Count != ids.Count)
        {
            return ApiResponse.Fail("Sıralama listesi geçersiz.");
        }

        var lookup = entities.ToDictionary(x => x.Id);
        var utcNow = DateTime.UtcNow;
        for (var i = 0; i < ids.Count; i++)
        {
            var entity = lookup[ids[i]];
            entity.SortOrder = i;
            entity.UpdatedAt = utcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse.Ok("Sıralama güncellendi.");
    }

    public async Task<ApiResponse<ApplyDryLubePriceIncreaseResultDto>> ApplyPriceIncreaseAsync(
        ApplyDryLubePriceIncreaseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Percent == 0)
        {
            return ApiResponse<ApplyDryLubePriceIncreaseResultDto>.Fail("Zam oranı 0 olamaz.");
        }

        var scope = (request.Scope ?? "all").Trim().ToLowerInvariant();
        string? serviceKey = null;

        if (scope == "service")
        {
            serviceKey = request.ServiceKey?.Trim();
            if (string.IsNullOrWhiteSpace(serviceKey) || !ServiceKeys.Contains(serviceKey))
            {
                return ApiResponse<ApplyDryLubePriceIncreaseResultDto>.Fail("Geçerli bir hizmet seçilmelidir.");
            }
        }
        else if (scope != "all")
        {
            return ApiResponse<ApplyDryLubePriceIncreaseResultDto>.Fail("Geçersiz zam kapsamı.");
        }

        var items = await _db.DryLubePrices.ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            return ApiResponse<ApplyDryLubePriceIncreaseResultDto>.Fail("Güncellenecek kayıt bulunamadı.");
        }

        var factor = 1 + (request.Percent / 100m);
        var utcNow = DateTime.UtcNow;
        var updatedFields = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            var rowChanged = false;

            bool ApplyOne(ref decimal? value)
            {
                if (ServicePriceIncreaseHelper.TryApply(ref value, factor, out var wasSkipped))
                {
                    updatedFields++;
                    return true;
                }

                if (wasSkipped)
                {
                    skipped++;
                }

                return false;
            }

            void ApplyService(string key, Action apply)
            {
                if (scope != "all" && !string.Equals(serviceKey, key, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                apply();
            }

            ApplyService("normal", () =>
            {
                var card = item.CardNormal;
                var cash = item.CashNormal;
                if (ApplyOne(ref card)) rowChanged = true;
                if (ApplyOne(ref cash)) rowChanged = true;
                item.CardNormal = card;
                item.CashNormal = cash;
            });

            ApplyService("waterless", () =>
            {
                var card = item.CardWaterless;
                var cash = item.CashWaterless;
                if (ApplyOne(ref card)) rowChanged = true;
                if (ApplyOne(ref cash)) rowChanged = true;
                item.CardWaterless = card;
                item.CashWaterless = cash;
            });

            ApplyService("underWashDryLube", () =>
            {
                var card = item.CardUnderWashDryLube;
                var cash = item.CashUnderWashDryLube;
                if (ApplyOne(ref card)) rowChanged = true;
                if (ApplyOne(ref cash)) rowChanged = true;
                item.CardUnderWashDryLube = card;
                item.CashUnderWashDryLube = cash;
            });

            if (rowChanged)
            {
                item.LastPriceDate = utcNow;
                item.UpdatedAt = utcNow;
            }
        }

        if (updatedFields == 0)
        {
            return ApiResponse<ApplyDryLubePriceIncreaseResultDto>.Fail(
                "Fiyatı güncellenecek alan bulunamadı. Boş fiyatlar atlanır.");
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<ApplyDryLubePriceIncreaseResultDto>.Ok(
            new ApplyDryLubePriceIncreaseResultDto
            {
                UpdatedCount = updatedFields,
                SkippedCount = skipped
            },
            $"{updatedFields} fiyat alanı güncellendi.");
    }

    private static bool HasNegative(CreateDryLubePriceRequest request) =>
        request.CardNormal is < 0 || request.CashNormal is < 0 ||
        request.CardWaterless is < 0 || request.CashWaterless is < 0 ||
        request.CardUnderWashDryLube is < 0 || request.CashUnderWashDryLube is < 0;

    private static void ApplyPrices(DryLubePrice entity, CreateDryLubePriceRequest request)
    {
        entity.CardNormal = request.CardNormal;
        entity.CashNormal = request.CashNormal;
        entity.CardWaterless = request.CardWaterless;
        entity.CashWaterless = request.CashWaterless;
        entity.CardUnderWashDryLube = request.CardUnderWashDryLube;
        entity.CashUnderWashDryLube = request.CashUnderWashDryLube;
    }

    private static bool HasAnyPrice(DryLubePrice e) =>
        ServicePriceIncreaseHelper.HasAnyPrice(
            e.CardNormal, e.CashNormal, e.CardWaterless, e.CashWaterless,
            e.CardUnderWashDryLube, e.CashUnderWashDryLube);

    private static decimal?[] Snapshot(DryLubePrice e) =>
    [
        e.CardNormal, e.CashNormal, e.CardWaterless, e.CashWaterless,
        e.CardUnderWashDryLube, e.CashUnderWashDryLube
    ];

    private static bool PricesDiffer(decimal?[] a, decimal?[] b) =>
        a.Zip(b, (x, y) => x != y).Any(diff => diff);

    private static DryLubePriceDto Map(DryLubePrice entity) => new()
    {
        Id = entity.Id,
        BrandModel = entity.BrandModel,
        SortOrder = entity.SortOrder,
        LastPriceDate = entity.LastPriceDate,
        CardNormal = entity.CardNormal,
        CashNormal = entity.CashNormal,
        CardWaterless = entity.CardWaterless,
        CashWaterless = entity.CashWaterless,
        CardUnderWashDryLube = entity.CardUnderWashDryLube,
        CashUnderWashDryLube = entity.CashUnderWashDryLube,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
