using Bahoto.Application.Common;
using Bahoto.Application.DTOs.WashPrice;
using Bahoto.Application.Interfaces;
using Bahoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bahoto.Application.Services;

public class WashPriceService : IWashPriceService
{
    private static readonly HashSet<string> ServiceKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "interiorExterior",
        "exterior",
        "underWash",
        "underEngine",
        "underOverEngine",
        "overEngine",
        "underWashEngine",
        "fullWash"
    };

    private readonly IApplicationDbContext _db;

    public WashPriceService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<WashPriceListData>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.WashPrices.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.BrandModel)
            .ToListAsync(cancellationToken);

        return ApiResponse<WashPriceListData>.Ok(new WashPriceListData
        {
            Items = items.Select(Map).ToList(),
            TableLastPriceDate = items.Count == 0 ? null : items.Max(x => x.LastPriceDate)
        });
    }

    public async Task<ApiResponse<WashPriceDto>> CreateAsync(CreateWashPriceRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.BrandModel))
        {
            return ApiResponse<WashPriceDto>.Fail("Marka-Model zorunludur.");
        }

        if (HasNegative(request))
        {
            return ApiResponse<WashPriceDto>.Fail("Fiyat negatif olamaz.");
        }

        var brandModel = request.BrandModel.Trim();
        var exists = await _db.WashPrices.AnyAsync(
            x => x.BrandModel.ToLower() == brandModel.ToLower(),
            cancellationToken);

        if (exists)
        {
            return ApiResponse<WashPriceDto>.Fail("Bu Marka-Model zaten kayıtlı.");
        }

        var maxSort = await _db.WashPrices.MaxAsync(x => (int?)x.SortOrder, cancellationToken) ?? -1;
        var entity = new WashPrice
        {
            BrandModel = brandModel,
            SortOrder = maxSort + 1
        };
        ApplyPrices(entity, request);

        if (HasAnyPrice(entity))
        {
            entity.LastPriceDate = DateTime.UtcNow;
        }

        _db.WashPrices.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<WashPriceDto>.Ok(Map(entity), "Yıkama fiyatı eklendi.");
    }

    public async Task<ApiResponse<WashPriceDto>> UpdateAsync(UpdateWashPriceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.WashPrices.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return ApiResponse<WashPriceDto>.Fail("Kayıt bulunamadı.");
        }

        if (string.IsNullOrWhiteSpace(request.BrandModel))
        {
            return ApiResponse<WashPriceDto>.Fail("Marka-Model zorunludur.");
        }

        if (HasNegative(request))
        {
            return ApiResponse<WashPriceDto>.Fail("Fiyat negatif olamaz.");
        }

        var brandModel = request.BrandModel.Trim();
        var duplicate = await _db.WashPrices.AnyAsync(
            x => x.Id != request.Id && x.BrandModel.ToLower() == brandModel.ToLower(),
            cancellationToken);

        if (duplicate)
        {
            return ApiResponse<WashPriceDto>.Fail("Bu Marka-Model zaten kayıtlı.");
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

        return ApiResponse<WashPriceDto>.Ok(Map(entity), "Yıkama fiyatı güncellendi.");
    }

    public async Task<ApiResponse> DeleteAsync(DeleteWashPriceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.WashPrices.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return ApiResponse.Fail("Kayıt bulunamadı.");
        }

        var utcNow = DateTime.UtcNow;
        entity.DeletedAt = utcNow;
        entity.UpdatedAt = utcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Yıkama fiyatı silindi.");
    }

    public async Task<ApiResponse> ReorderAsync(ReorderWashPriceRequest request, CancellationToken cancellationToken = default)
    {
        var ids = request.Ids.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count < 2)
        {
            return ApiResponse.Fail("Sıralama için en az iki kayıt gerekir.");
        }

        var entities = await _db.WashPrices
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

    public async Task<ApiResponse<ApplyServicePriceIncreaseResultDto>> ApplyPriceIncreaseAsync(
        ApplyWashPriceIncreaseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Percent == 0)
        {
            return ApiResponse<ApplyServicePriceIncreaseResultDto>.Fail("Zam oranı 0 olamaz.");
        }

        var scope = (request.Scope ?? "all").Trim().ToLowerInvariant();
        string? serviceKey = null;

        if (scope == "service")
        {
            serviceKey = request.ServiceKey?.Trim();
            if (string.IsNullOrWhiteSpace(serviceKey) || !ServiceKeys.Contains(serviceKey))
            {
                return ApiResponse<ApplyServicePriceIncreaseResultDto>.Fail("Geçerli bir hizmet seçilmelidir.");
            }
        }
        else if (scope != "all")
        {
            return ApiResponse<ApplyServicePriceIncreaseResultDto>.Fail("Geçersiz zam kapsamı.");
        }

        var items = await _db.WashPrices.ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            return ApiResponse<ApplyServicePriceIncreaseResultDto>.Fail("Güncellenecek kayıt bulunamadı.");
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

            ApplyService("interiorExterior", () =>
            {
                var card = item.CardInteriorExterior;
                var cash = item.CashInteriorExterior;
                if (ApplyOne(ref card)) rowChanged = true;
                if (ApplyOne(ref cash)) rowChanged = true;
                item.CardInteriorExterior = card;
                item.CashInteriorExterior = cash;
            });

            ApplyService("exterior", () =>
            {
                var card = item.CardExterior;
                var cash = item.CashExterior;
                if (ApplyOne(ref card)) rowChanged = true;
                if (ApplyOne(ref cash)) rowChanged = true;
                item.CardExterior = card;
                item.CashExterior = cash;
            });

            ApplyService("underWash", () =>
            {
                var card = item.CardUnderWash;
                var cash = item.CashUnderWash;
                if (ApplyOne(ref card)) rowChanged = true;
                if (ApplyOne(ref cash)) rowChanged = true;
                item.CardUnderWash = card;
                item.CashUnderWash = cash;
            });

            ApplyService("underEngine", () =>
            {
                var card = item.CardUnderEngine;
                var cash = item.CashUnderEngine;
                if (ApplyOne(ref card)) rowChanged = true;
                if (ApplyOne(ref cash)) rowChanged = true;
                item.CardUnderEngine = card;
                item.CashUnderEngine = cash;
            });

            ApplyService("underOverEngine", () =>
            {
                var card = item.CardUnderOverEngine;
                var cash = item.CashUnderOverEngine;
                if (ApplyOne(ref card)) rowChanged = true;
                if (ApplyOne(ref cash)) rowChanged = true;
                item.CardUnderOverEngine = card;
                item.CashUnderOverEngine = cash;
            });

            ApplyService("overEngine", () =>
            {
                var card = item.CardOverEngine;
                var cash = item.CashOverEngine;
                if (ApplyOne(ref card)) rowChanged = true;
                if (ApplyOne(ref cash)) rowChanged = true;
                item.CardOverEngine = card;
                item.CashOverEngine = cash;
            });

            ApplyService("underWashEngine", () =>
            {
                var card = item.CardUnderWashEngine;
                var cash = item.CashUnderWashEngine;
                if (ApplyOne(ref card)) rowChanged = true;
                if (ApplyOne(ref cash)) rowChanged = true;
                item.CardUnderWashEngine = card;
                item.CashUnderWashEngine = cash;
            });

            ApplyService("fullWash", () =>
            {
                var card = item.CardFullWash;
                var cash = item.CashFullWash;
                if (ApplyOne(ref card)) rowChanged = true;
                if (ApplyOne(ref cash)) rowChanged = true;
                item.CardFullWash = card;
                item.CashFullWash = cash;
            });

            if (rowChanged)
            {
                item.LastPriceDate = utcNow;
                item.UpdatedAt = utcNow;
            }
        }

        if (updatedFields == 0)
        {
            return ApiResponse<ApplyServicePriceIncreaseResultDto>.Fail(
                "Fiyatı güncellenecek alan bulunamadı. Boş fiyatlar atlanır.");
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<ApplyServicePriceIncreaseResultDto>.Ok(
            new ApplyServicePriceIncreaseResultDto
            {
                UpdatedCount = updatedFields,
                SkippedCount = skipped
            },
            $"{updatedFields} fiyat alanı güncellendi.");
    }

    private static bool HasNegative(CreateWashPriceRequest request) =>
        request.CardInteriorExterior is < 0 || request.CashInteriorExterior is < 0 ||
        request.CardExterior is < 0 || request.CashExterior is < 0 ||
        request.CardUnderWash is < 0 || request.CashUnderWash is < 0 ||
        request.CardUnderEngine is < 0 || request.CashUnderEngine is < 0 ||
        request.CardUnderOverEngine is < 0 || request.CashUnderOverEngine is < 0 ||
        request.CardOverEngine is < 0 || request.CashOverEngine is < 0 ||
        request.CardUnderWashEngine is < 0 || request.CashUnderWashEngine is < 0 ||
        request.CardFullWash is < 0 || request.CashFullWash is < 0;

    private static void ApplyPrices(WashPrice entity, CreateWashPriceRequest request)
    {
        entity.CardInteriorExterior = request.CardInteriorExterior;
        entity.CashInteriorExterior = request.CashInteriorExterior;
        entity.CardExterior = request.CardExterior;
        entity.CashExterior = request.CashExterior;
        entity.CardUnderWash = request.CardUnderWash;
        entity.CashUnderWash = request.CashUnderWash;
        entity.CardUnderEngine = request.CardUnderEngine;
        entity.CashUnderEngine = request.CashUnderEngine;
        entity.CardUnderOverEngine = request.CardUnderOverEngine;
        entity.CashUnderOverEngine = request.CashUnderOverEngine;
        entity.CardOverEngine = request.CardOverEngine;
        entity.CashOverEngine = request.CashOverEngine;
        entity.CardUnderWashEngine = request.CardUnderWashEngine;
        entity.CashUnderWashEngine = request.CashUnderWashEngine;
        entity.CardFullWash = request.CardFullWash;
        entity.CashFullWash = request.CashFullWash;
    }

    private static bool HasAnyPrice(WashPrice e) =>
        ServicePriceIncreaseHelper.HasAnyPrice(
            e.CardInteriorExterior, e.CashInteriorExterior, e.CardExterior, e.CashExterior,
            e.CardUnderWash, e.CashUnderWash, e.CardUnderEngine, e.CashUnderEngine,
            e.CardUnderOverEngine, e.CashUnderOverEngine, e.CardOverEngine, e.CashOverEngine,
            e.CardUnderWashEngine, e.CashUnderWashEngine, e.CardFullWash, e.CashFullWash);

    private static decimal?[] Snapshot(WashPrice e) =>
    [
        e.CardInteriorExterior, e.CashInteriorExterior, e.CardExterior, e.CashExterior,
        e.CardUnderWash, e.CashUnderWash, e.CardUnderEngine, e.CashUnderEngine,
        e.CardUnderOverEngine, e.CashUnderOverEngine, e.CardOverEngine, e.CashOverEngine,
        e.CardUnderWashEngine, e.CashUnderWashEngine, e.CardFullWash, e.CashFullWash
    ];

    private static bool PricesDiffer(decimal?[] a, decimal?[] b) =>
        a.Zip(b, (x, y) => x != y).Any(diff => diff);

    private static WashPriceDto Map(WashPrice entity) => new()
    {
        Id = entity.Id,
        BrandModel = entity.BrandModel,
        SortOrder = entity.SortOrder,
        LastPriceDate = entity.LastPriceDate,
        CardInteriorExterior = entity.CardInteriorExterior,
        CashInteriorExterior = entity.CashInteriorExterior,
        CardExterior = entity.CardExterior,
        CashExterior = entity.CashExterior,
        CardUnderWash = entity.CardUnderWash,
        CashUnderWash = entity.CashUnderWash,
        CardUnderEngine = entity.CardUnderEngine,
        CashUnderEngine = entity.CashUnderEngine,
        CardUnderOverEngine = entity.CardUnderOverEngine,
        CashUnderOverEngine = entity.CashUnderOverEngine,
        CardOverEngine = entity.CardOverEngine,
        CashOverEngine = entity.CashOverEngine,
        CardUnderWashEngine = entity.CardUnderWashEngine,
        CashUnderWashEngine = entity.CashUnderWashEngine,
        CardFullWash = entity.CardFullWash,
        CashFullWash = entity.CashFullWash,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
