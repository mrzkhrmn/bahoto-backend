using System.Globalization;
using System.Text.RegularExpressions;
using Bahoto.Application.Common;
using Bahoto.Application.DTOs.VehicleMaintenance;
using Bahoto.Application.Interfaces;
using Bahoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bahoto.Application.Services;

public class VehicleMaintenanceService : IVehicleMaintenanceService
{
    private static readonly CultureInfo Tr = new("tr-TR");

    private readonly IApplicationDbContext _db;

    public VehicleMaintenanceService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<VehicleMaintenanceListData>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.VehicleMaintenances.AsNoTracking().ToListAsync(cancellationToken);
        var ordered = OrderByBrandGroups(items);

        return ApiResponse<VehicleMaintenanceListData>.Ok(new VehicleMaintenanceListData
        {
            Items = ordered.Select(Map).ToList()
        });
    }

    public async Task<ApiResponse<VehicleMaintenanceDto>> CreateAsync(
        CreateVehicleMaintenanceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.BrandModel))
        {
            return ApiResponse<VehicleMaintenanceDto>.Fail("Marka-Model zorunludur.");
        }

        var brandModel = NormalizeBrandModel(request.BrandModel);
        var brandModelLower = brandModel.ToLower(Tr);
        var exists = await _db.VehicleMaintenances.AnyAsync(
            x => x.BrandModel.ToLower() == brandModelLower,
            cancellationToken);

        if (exists)
        {
            return ApiResponse<VehicleMaintenanceDto>.Fail("Bu Marka-Model zaten kayıtlı.");
        }

        var all = await _db.VehicleMaintenances
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var entity = new VehicleMaintenance
        {
            BrandModel = brandModel,
            EngineOil = Normalize(request.EngineOil),
            AirFilter = Normalize(request.AirFilter),
            OilFilter = Normalize(request.OilFilter),
            PolenFilter = Normalize(request.PolenFilter)
        };

        PlaceInBrandGroupAndRenumber(all, entity);
        _db.VehicleMaintenances.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<VehicleMaintenanceDto>.Ok(Map(entity), "Araç bakım kaydı eklendi.");
    }

    public async Task<ApiResponse<VehicleMaintenanceDto>> UpdateAsync(
        UpdateVehicleMaintenanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.VehicleMaintenances.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return ApiResponse<VehicleMaintenanceDto>.Fail("Kayıt bulunamadı.");
        }

        if (string.IsNullOrWhiteSpace(request.BrandModel))
        {
            return ApiResponse<VehicleMaintenanceDto>.Fail("Marka-Model zorunludur.");
        }

        var brandModel = NormalizeBrandModel(request.BrandModel);
        var brandModelLower = brandModel.ToLower(Tr);
        var duplicate = await _db.VehicleMaintenances.AnyAsync(
            x => x.Id != request.Id && x.BrandModel.ToLower() == brandModelLower,
            cancellationToken);

        if (duplicate)
        {
            return ApiResponse<VehicleMaintenanceDto>.Fail("Bu Marka-Model zaten kayıtlı.");
        }

        var oldBrand = ExtractBrandKey(entity.BrandModel);
        var newBrand = ExtractBrandKey(brandModel);
        entity.BrandModel = brandModel;
        entity.EngineOil = Normalize(request.EngineOil);
        entity.AirFilter = Normalize(request.AirFilter);
        entity.OilFilter = Normalize(request.OilFilter);
        entity.PolenFilter = Normalize(request.PolenFilter);
        entity.UpdatedAt = DateTime.UtcNow;

        if (!string.Equals(oldBrand, newBrand, StringComparison.Ordinal))
        {
            var others = await _db.VehicleMaintenances
                .Where(x => x.Id != entity.Id)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            PlaceInBrandGroupAndRenumber(others, entity);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<VehicleMaintenanceDto>.Ok(Map(entity), "Araç bakım kaydı güncellendi.");
    }

    public async Task<ApiResponse> DeleteAsync(
        DeleteVehicleMaintenanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.VehicleMaintenances.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return ApiResponse.Fail("Kayıt bulunamadı.");
        }

        var utcNow = DateTime.UtcNow;
        entity.DeletedAt = utcNow;
        entity.UpdatedAt = utcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Araç bakım kaydı silindi.");
    }

    /// <summary>
    /// Yeni kaydı aynı markanın (ilk kelime) son satırının hemen altına koyar,
    /// tüm SortOrder değerlerini 0..n olarak yeniden yazar.
    /// </summary>
    private static void PlaceInBrandGroupAndRenumber(
        List<VehicleMaintenance> existingOrdered,
        VehicleMaintenance entity)
    {
        var brand = ExtractBrandKey(entity.BrandModel);
        var result = new List<VehicleMaintenance>(existingOrdered.Count + 1);
        var inserted = false;

        for (var i = 0; i < existingOrdered.Count; i++)
        {
            result.Add(existingOrdered[i]);

            var isLastOfBrand =
                string.Equals(ExtractBrandKey(existingOrdered[i].BrandModel), brand, StringComparison.Ordinal)
                && (i == existingOrdered.Count - 1
                    || !string.Equals(
                        ExtractBrandKey(existingOrdered[i + 1].BrandModel),
                        brand,
                        StringComparison.Ordinal));

            if (isLastOfBrand)
            {
                result.Add(entity);
                inserted = true;
            }
        }

        if (!inserted)
        {
            result.Add(entity);
        }

        for (var i = 0; i < result.Count; i++)
        {
            result[i].SortOrder = i;
        }
    }

    private static List<VehicleMaintenance> OrderByBrandGroups(List<VehicleMaintenance> items)
    {
        if (items.Count == 0) return items;

        var brandFirstSort = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            var brand = ExtractBrandKey(item.BrandModel);
            if (!brandFirstSort.TryGetValue(brand, out var first) || item.SortOrder < first)
            {
                brandFirstSort[brand] = item.SortOrder;
            }
        }

        return items
            .OrderBy(x => brandFirstSort[ExtractBrandKey(x.BrandModel)])
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.BrandModel, StringComparer.Create(Tr, ignoreCase: true))
            .ToList();
    }

    private static string NormalizeBrandModel(string brandModel) =>
        Regex.Replace(brandModel.Trim(), @"\s+", " ");

    /// <summary>Marka = Marka-Model alanındaki ilk kelime (Fiat Doblo → fiat).</summary>
    private static string ExtractBrandKey(string brandModel)
    {
        var normalized = NormalizeBrandModel(brandModel);
        if (normalized.Length == 0) return string.Empty;

        var space = normalized.IndexOf(' ');
        var brand = space < 0 ? normalized : normalized[..space];
        return brand.ToLower(Tr);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim();
    }

    private static VehicleMaintenanceDto Map(VehicleMaintenance entity) => new()
    {
        Id = entity.Id,
        BrandModel = entity.BrandModel,
        SortOrder = entity.SortOrder,
        EngineOil = entity.EngineOil,
        AirFilter = entity.AirFilter,
        OilFilter = entity.OilFilter,
        PolenFilter = entity.PolenFilter,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
