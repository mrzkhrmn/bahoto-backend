using Bahoto.Application.Common;
using Bahoto.Application.DTOs.OilChange;
using Bahoto.Application.Interfaces;
using Bahoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bahoto.Application.Services;

public class OilChangeService : IOilChangeService
{
    private static readonly HashSet<string> AllowedPaymentMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Nakit",
        "KK",
        "C Iban",
        "M Iban",
        // Eski kayıt uyumu
        "Kredi Kartı"
    };

    private readonly IApplicationDbContext _db;

    public OilChangeService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<PagedResult<OilChangeDto>>> ListAsync(ListOilChangeRequest request, CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var query = _db.OilChanges.AsNoTracking().OrderByDescending(x => x.CreatedAt);
        var totalCount = await query.CountAsync(cancellationToken);
        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<OilChangeDto>>.Ok(new PagedResult<OilChangeDto>
        {
            Items = entities.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    public async Task<ApiResponse<OilChangeDto>> GetAsync(GetOilChangeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.OilChanges.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return ApiResponse<OilChangeDto>.Fail("Kayıt bulunamadı.");
        }

        return ApiResponse<OilChangeDto>.Ok(Map(entity));
    }

    public async Task<ApiResponse<OilChangeDto>> CreateAsync(CreateOilChangeRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.OilType))
        {
            return ApiResponse<OilChangeDto>.Fail("OilType zorunludur.");
        }

        if (request.KmChanged < 0)
        {
            return ApiResponse<OilChangeDto>.Fail("KmChanged geçerli bir değer olmalıdır.");
        }

        if (request.Price < 0)
        {
            return ApiResponse<OilChangeDto>.Fail("Price geçerli bir değer olmalıdır.");
        }

        var paymentError = ValidatePayment(request.PaymentMethod, out var paymentMethod);
        if (paymentError is not null)
        {
            return ApiResponse<OilChangeDto>.Fail(paymentError);
        }

        var entity = new OilChange
        {
            Vehicle = request.Vehicle,
            Plate = request.Plate,
            Phone = request.Phone,
            OilType = request.OilType.Trim(),
            KmChanged = request.KmChanged,
            NextChangeKm = request.NextChangeKm ?? request.KmChanged + 10000,
            OilFilter = request.OilFilter,
            AirFilter = request.AirFilter,
            FuelFilter = request.FuelFilter,
            PolenFilter = request.PolenFilter,
            Note = request.Note,
            Employee = request.Employee,
            Price = request.Price,
            PaymentMethod = paymentMethod,
            BankAccount = NormalizeOptional(request.BankAccount)
        };

        _db.OilChanges.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<OilChangeDto>.Ok(Map(entity), "Kayıt oluşturuldu.");
    }

    public async Task<ApiResponse<OilChangeDto>> UpdateAsync(UpdateOilChangeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.OilChanges.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return ApiResponse<OilChangeDto>.Fail("Kayıt bulunamadı.");
        }

        if (string.IsNullOrWhiteSpace(request.OilType))
        {
            return ApiResponse<OilChangeDto>.Fail("OilType zorunludur.");
        }

        if (request.KmChanged < 0 || request.NextChangeKm < 0)
        {
            return ApiResponse<OilChangeDto>.Fail("Km değerleri geçerli olmalıdır.");
        }

        if (request.Price < 0)
        {
            return ApiResponse<OilChangeDto>.Fail("Price geçerli bir değer olmalıdır.");
        }

        var paymentError = ValidatePayment(request.PaymentMethod, out var paymentMethod);
        if (paymentError is not null)
        {
            return ApiResponse<OilChangeDto>.Fail(paymentError);
        }

        entity.Vehicle = request.Vehicle;
        entity.Plate = request.Plate;
        entity.Phone = request.Phone;
        entity.OilType = request.OilType.Trim();
        entity.KmChanged = request.KmChanged;
        entity.NextChangeKm = request.NextChangeKm;
        entity.OilFilter = request.OilFilter;
        entity.AirFilter = request.AirFilter;
        entity.FuelFilter = request.FuelFilter;
        entity.PolenFilter = request.PolenFilter;
        entity.Note = request.Note;
        entity.Employee = request.Employee;
        entity.Price = request.Price;
        entity.PaymentMethod = paymentMethod;
        entity.BankAccount = NormalizeOptional(request.BankAccount);
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<OilChangeDto>.Ok(Map(entity), "Kayıt güncellendi.");
    }

    public async Task<ApiResponse> DeleteAsync(DeleteOilChangeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.OilChanges.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return ApiResponse.Fail("Kayıt bulunamadı.");
        }

        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Kayıt silindi.");
    }

    private static string? ValidatePayment(string? paymentMethod, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            return null;
        }

        var trimmed = paymentMethod.Trim();
        if (!AllowedPaymentMethods.Contains(trimmed))
        {
            return "Ödeme tipi Nakit, KK, C Iban veya M Iban olmalıdır.";
        }

        if (trimmed.Equals("Kredi Kartı", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("KK", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "KK";
        }
        else if (trimmed.Equals("C Iban", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "C Iban";
        }
        else if (trimmed.Equals("M Iban", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "M Iban";
        }
        else
        {
            normalized = "Nakit";
        }

        return null;
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static OilChangeDto Map(OilChange entity) => new()
    {
        Id = entity.Id,
        Vehicle = entity.Vehicle,
        Plate = entity.Plate,
        Phone = entity.Phone,
        OilType = entity.OilType,
        KmChanged = entity.KmChanged,
        NextChangeKm = entity.NextChangeKm,
        OilFilter = entity.OilFilter,
        AirFilter = entity.AirFilter,
        FuelFilter = entity.FuelFilter,
        PolenFilter = entity.PolenFilter,
        Note = entity.Note,
        Employee = entity.Employee,
        Price = entity.Price,
        PaymentMethod = entity.PaymentMethod,
        BankAccount = entity.BankAccount,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
