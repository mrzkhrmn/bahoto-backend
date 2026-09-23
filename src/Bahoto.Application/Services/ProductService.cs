using Bahoto.Application.Common;
using Bahoto.Application.DTOs.OilChange;
using Bahoto.Application.DTOs.Product;
using Bahoto.Application.Interfaces;
using Bahoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bahoto.Application.Services;

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _db;

    public ProductService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<PagedResult<ProductDto>>> ListAsync(ListProductRequest request, CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 1000);

        var query = _db.Products.AsNoTracking().OrderByDescending(x => x.CreatedAt);
        var totalCount = await query.CountAsync(cancellationToken);
        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<ProductDto>>.Ok(new PagedResult<ProductDto>
        {
            Items = entities.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    public async Task<ApiResponse<ProductDto>> GetAsync(GetProductRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Products.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return ApiResponse<ProductDto>.Fail("Ürün bulunamadı.");
        }

        return ApiResponse<ProductDto>.Ok(Map(entity));
    }

    public async Task<ApiResponse<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Brand))
        {
            return ApiResponse<ProductDto>.Fail("Marka zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiResponse<ProductDto>.Fail("Ürün adı zorunludur.");
        }

        var entity = new Product
        {
            Brand = request.Brand.Trim(),
            Name = request.Name.Trim()
        };

        _db.Products.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<ProductDto>.Ok(Map(entity), "Ürün oluşturuldu.");
    }

    public async Task<ApiResponse<ProductDto>> UpdateAsync(UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Products.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return ApiResponse<ProductDto>.Fail("Ürün bulunamadı.");
        }

        if (string.IsNullOrWhiteSpace(request.Brand))
        {
            return ApiResponse<ProductDto>.Fail("Marka zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiResponse<ProductDto>.Fail("Ürün adı zorunludur.");
        }

        entity.Brand = request.Brand.Trim();
        entity.Name = request.Name.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<ProductDto>.Ok(Map(entity), "Ürün güncellendi.");
    }

    public async Task<ApiResponse> DeleteAsync(DeleteProductRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Products.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return ApiResponse.Fail("Ürün bulunamadı.");
        }

        var hasCari = await _db.Caris.AnyAsync(x => x.ProductId == request.Id, cancellationToken);
        if (hasCari)
        {
            return ApiResponse.Fail("Bu ürüne bağlı cari kayıtları olduğu için silinemez.");
        }

        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Ürün silindi.");
    }

    private static ProductDto Map(Product entity) => new()
    {
        Id = entity.Id,
        Brand = entity.Brand,
        Name = entity.Name,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
