using Bahoto.Application.Common;
using Bahoto.Application.DTOs.Cari;
using Bahoto.Application.DTOs.OilChange;
using Bahoto.Application.Interfaces;
using Bahoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bahoto.Application.Services;

public class CariService : ICariService
{
    private readonly IApplicationDbContext _db;

    public CariService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<PagedResult<CariDto>>> ListAsync(ListCariRequest request, CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 1000);

        var query = _db.Caris
            .AsNoTracking()
            .Include(x => x.Product)
            .OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<CariDto>>.Ok(new PagedResult<CariDto>
        {
            Items = entities.Select(Map).ToList(),
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
        var validationError = ValidateAmounts(request.IncomingAmount, request.PaidAmount);
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

        var entity = new Cari
        {
            ProductId = request.ProductId,
            IncomingAmount = request.IncomingAmount,
            PaidAmount = request.PaidAmount,
            Balance = request.IncomingAmount - request.PaidAmount
        };

        _db.Caris.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        entity.Product = product;
        return ApiResponse<CariDto>.Ok(Map(entity), "Cari kaydı oluşturuldu.");
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

        var validationError = ValidateAmounts(request.IncomingAmount, request.PaidAmount);
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

    private static string? ValidateAmounts(decimal incomingAmount, decimal paidAmount)
    {
        if (incomingAmount < 0)
        {
            return "Gelen miktar geçerli bir değer olmalıdır.";
        }

        if (paidAmount < 0)
        {
            return "Ödenen miktar geçerli bir değer olmalıdır.";
        }

        return null;
    }

    private static CariDto Map(Cari entity) => new()
    {
        Id = entity.Id,
        ProductId = entity.ProductId,
        ProductBrand = entity.Product?.Brand ?? string.Empty,
        ProductName = entity.Product?.Name ?? string.Empty,
        IncomingAmount = entity.IncomingAmount,
        PaidAmount = entity.PaidAmount,
        Balance = entity.Balance,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
