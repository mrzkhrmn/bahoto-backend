using Bahoto.Application.Common;
using Bahoto.Application.DTOs.KarmaProduct;
using Bahoto.Application.Interfaces;
using Bahoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bahoto.Application.Services;

public class KarmaProductService : IKarmaProductService
{
    private readonly IApplicationDbContext _db;

    public KarmaProductService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<KarmaProductListData>> ListAsync(
        ListKarmaProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _db.KarmaProducts.AsNoTracking();
        var search = request.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(x =>
                x.Brand.ToLower().Contains(term) ||
                x.Name.ToLower().Contains(term));
        }

        var items = await query
            .OrderBy(x => x.Brand)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return ApiResponse<KarmaProductListData>.Ok(new KarmaProductListData
        {
            Items = items.Select(Map).ToList()
        });
    }

    public async Task<ApiResponse<KarmaProductDto>> CreateAsync(
        CreateKarmaProductRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Brand))
        {
            return ApiResponse<KarmaProductDto>.Fail("Marka zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiResponse<KarmaProductDto>.Fail("Ürün adı zorunludur.");
        }

        if (request.Price is < 0)
        {
            return ApiResponse<KarmaProductDto>.Fail("Fiyat negatif olamaz.");
        }

        var brand = request.Brand.Trim();
        var name = request.Name.Trim();

        var exists = await _db.KarmaProducts.AnyAsync(
            x => x.Brand.ToLower() == brand.ToLower() && x.Name.ToLower() == name.ToLower(),
            cancellationToken);

        if (exists)
        {
            return ApiResponse<KarmaProductDto>.Fail("Bu marka ve ürün adı zaten kayıtlı.");
        }

        var entity = new KarmaProduct
        {
            Brand = brand,
            Name = name,
            Price = request.Price,
            LastPriceDate = request.Price.HasValue ? DateTime.UtcNow : null
        };

        _db.KarmaProducts.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<KarmaProductDto>.Ok(Map(entity), "Karma ürün oluşturuldu.");
    }

    public async Task<ApiResponse<KarmaProductDto>> UpdateAsync(
        UpdateKarmaProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.KarmaProducts.FirstOrDefaultAsync(
            x => x.Id == request.Id,
            cancellationToken);

        if (entity is null)
        {
            return ApiResponse<KarmaProductDto>.Fail("Karma ürün bulunamadı.");
        }

        if (string.IsNullOrWhiteSpace(request.Brand))
        {
            return ApiResponse<KarmaProductDto>.Fail("Marka zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiResponse<KarmaProductDto>.Fail("Ürün adı zorunludur.");
        }

        if (request.Price is < 0)
        {
            return ApiResponse<KarmaProductDto>.Fail("Fiyat negatif olamaz.");
        }

        var brand = request.Brand.Trim();
        var name = request.Name.Trim();

        var duplicate = await _db.KarmaProducts.AnyAsync(
            x => x.Id != request.Id &&
                 x.Brand.ToLower() == brand.ToLower() &&
                 x.Name.ToLower() == name.ToLower(),
            cancellationToken);

        if (duplicate)
        {
            return ApiResponse<KarmaProductDto>.Fail("Bu marka ve ürün adı zaten kayıtlı.");
        }

        entity.Brand = brand;
        entity.Name = name;

        if (entity.Price != request.Price)
        {
            entity.Price = request.Price;
            entity.LastPriceDate = request.Price.HasValue ? DateTime.UtcNow : null;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<KarmaProductDto>.Ok(Map(entity), "Karma ürün güncellendi.");
    }

    public async Task<ApiResponse> DeleteAsync(
        DeleteKarmaProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.KarmaProducts.FirstOrDefaultAsync(
            x => x.Id == request.Id,
            cancellationToken);

        if (entity is null)
        {
            return ApiResponse.Fail("Karma ürün bulunamadı.");
        }

        var utcNow = DateTime.UtcNow;
        entity.DeletedAt = utcNow;
        entity.UpdatedAt = utcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Karma ürün silindi.");
    }

    private static KarmaProductDto Map(KarmaProduct entity) => new()
    {
        Id = entity.Id,
        Brand = entity.Brand,
        Name = entity.Name,
        Price = entity.Price,
        LastPriceDate = entity.LastPriceDate,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
