using Bahoto.Application.Common;
using Bahoto.Application.DTOs.VehicleMaintenance;
using Bahoto.Application.Interfaces;
using Bahoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bahoto.Application.Services;

public class VehicleMaintenanceService : IVehicleMaintenanceService
{
    private readonly IApplicationDbContext _db;

    public VehicleMaintenanceService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<VehicleMaintenanceListData>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.VehicleMaintenances.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.BrandModel)
            .ToListAsync(cancellationToken);

        return ApiResponse<VehicleMaintenanceListData>.Ok(new VehicleMaintenanceListData
        {
            Items = items.Select(Map).ToList()
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

        var brandModel = request.BrandModel.Trim();
        var exists = await _db.VehicleMaintenances.AnyAsync(
            x => x.BrandModel.ToLower() == brandModel.ToLower(),
            cancellationToken);

        if (exists)
        {
            return ApiResponse<VehicleMaintenanceDto>.Fail("Bu Marka-Model zaten kayıtlı.");
        }

        var maxSort = await _db.VehicleMaintenances.MaxAsync(x => (int?)x.SortOrder, cancellationToken) ?? -1;
        var entity = new VehicleMaintenance
        {
            BrandModel = brandModel,
            SortOrder = maxSort + 1,
            EngineOil = Normalize(request.EngineOil),
            AirFilter = Normalize(request.AirFilter),
            OilFilter = Normalize(request.OilFilter),
            PolenFilter = Normalize(request.PolenFilter)
        };

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

        var brandModel = request.BrandModel.Trim();
        var duplicate = await _db.VehicleMaintenances.AnyAsync(
            x => x.Id != request.Id && x.BrandModel.ToLower() == brandModel.ToLower(),
            cancellationToken);

        if (duplicate)
        {
            return ApiResponse<VehicleMaintenanceDto>.Fail("Bu Marka-Model zaten kayıtlı.");
        }

        entity.BrandModel = brandModel;
        entity.EngineOil = Normalize(request.EngineOil);
        entity.AirFilter = Normalize(request.AirFilter);
        entity.OilFilter = Normalize(request.OilFilter);
        entity.PolenFilter = Normalize(request.PolenFilter);
        entity.UpdatedAt = DateTime.UtcNow;

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
