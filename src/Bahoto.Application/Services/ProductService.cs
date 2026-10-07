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

    public async Task<ApiResponse<PagedResult<ProductBrandGroupDto>>> ListAsync(
        ListProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 1000);
        var search = request.Search?.Trim();

        var query = _db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(x =>
                x.Brand.ToLower().Contains(term) ||
                x.Name.ToLower().Contains(term));
        }

        var brandSummaries = await query
            .GroupBy(x => x.Brand)
            .Select(g => new
            {
                Brand = g.Key,
                ProductCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        await EnsureBrandOrdersAsync(brandSummaries.Select(x => x.Brand), cancellationToken);

        var orderLookup = await _db.BrandOrders.AsNoTracking()
            .ToDictionaryAsync(x => x.Brand, x => x.SortOrder, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var orderedBrands = brandSummaries
            .OrderBy(x => orderLookup.GetValueOrDefault(x.Brand, int.MaxValue))
            .ThenBy(x => x.Brand, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var totalCount = orderedBrands.Count;
        var pageBrands = orderedBrands
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var brandNames = pageBrands.Select(x => x.Brand).ToList();

        var products = await _db.Products.AsNoTracking()
            .Where(x => brandNames.Contains(x.Brand))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var searchTerm = search?.ToLower();

        var items = pageBrands
            .Select(x =>
            {
                var brandProducts = products.Where(p => p.Brand == x.Brand);
                var brandMatchesSearch = string.IsNullOrWhiteSpace(searchTerm) ||
                    x.Brand.ToLower().Contains(searchTerm);

                if (!brandMatchesSearch && !string.IsNullOrWhiteSpace(searchTerm))
                {
                    brandProducts = brandProducts.Where(p =>
                        p.Name.ToLower().Contains(searchTerm));
                }

                var mapped = brandProducts.Select(Map).ToList();
                return new ProductBrandGroupDto
                {
                    Brand = x.Brand,
                    ProductCount = mapped.Count,
                    Products = mapped
                };
            })
            .ToList();

        return ApiResponse<PagedResult<ProductBrandGroupDto>>.Ok(new PagedResult<ProductBrandGroupDto>
        {
            Items = items,
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

        if (request.Price is < 0)
        {
            return ApiResponse<ProductDto>.Fail("Fiyat negatif olamaz.");
        }

        var brand = request.Brand.Trim();
        var name = request.Name.Trim();

        var exists = await _db.Products.AnyAsync(
            x => x.Brand.ToLower() == brand.ToLower() && x.Name.ToLower() == name.ToLower(),
            cancellationToken);

        if (exists)
        {
            return ApiResponse<ProductDto>.Fail("Bu marka ve ürün adı zaten kayıtlı.");
        }

        var entity = new Product
        {
            Brand = brand,
            Name = name,
            Price = request.Price,
            LastPriceDate = request.Price.HasValue ? DateTime.UtcNow : null
        };

        _db.Products.Add(entity);
        await EnsureBrandOrdersAsync([brand], cancellationToken);
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

        if (request.Price is < 0)
        {
            return ApiResponse<ProductDto>.Fail("Fiyat negatif olamaz.");
        }

        var brand = request.Brand.Trim();
        var name = request.Name.Trim();
        var oldBrand = entity.Brand;

        var duplicate = await _db.Products.AnyAsync(
            x => x.Id != request.Id &&
                 x.Brand.ToLower() == brand.ToLower() &&
                 x.Name.ToLower() == name.ToLower(),
            cancellationToken);

        if (duplicate)
        {
            return ApiResponse<ProductDto>.Fail("Bu marka ve ürün adı zaten kayıtlı.");
        }

        entity.Brand = brand;
        entity.Name = name;

        if (entity.Price != request.Price)
        {
            entity.Price = request.Price;
            entity.LastPriceDate = request.Price.HasValue ? DateTime.UtcNow : null;
        }

        entity.UpdatedAt = DateTime.UtcNow;

        if (!string.Equals(oldBrand, brand, StringComparison.OrdinalIgnoreCase))
        {
            await EnsureBrandOrdersAsync([brand], cancellationToken);
            await CleanupBrandOrderIfUnusedAsync(oldBrand, cancellationToken);
        }

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

        var brand = entity.Brand;
        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await CleanupBrandOrderIfUnusedAsync(brand, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Ürün silindi.");
    }

    public async Task<ApiResponse> DeleteBrandAsync(DeleteBrandRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Brand))
        {
            return ApiResponse.Fail("Marka zorunludur.");
        }

        var brand = request.Brand.Trim();
        var products = await _db.Products
            .Where(x => x.Brand.ToLower() == brand.ToLower())
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
        {
            return ApiResponse.Fail("Marka bulunamadı.");
        }

        var productIds = products.Select(x => x.Id).ToList();
        var hasCari = await _db.Caris.AnyAsync(x => productIds.Contains(x.ProductId), cancellationToken);
        if (hasCari)
        {
            return ApiResponse.Fail("Bu markaya bağlı cari kayıtları olan ürünler olduğu için silinemez.");
        }

        var utcNow = DateTime.UtcNow;
        foreach (var product in products)
        {
            product.DeletedAt = utcNow;
            product.UpdatedAt = utcNow;
        }

        var brandOrder = await _db.BrandOrders
            .FirstOrDefaultAsync(x => x.Brand.ToLower() == brand.ToLower(), cancellationToken);
        if (brandOrder is not null)
        {
            brandOrder.DeletedAt = utcNow;
            brandOrder.UpdatedAt = utcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Marka ve ürünleri silindi.");
    }

    public async Task<ApiResponse<ApplyPriceIncreaseResultDto>> ApplyPriceIncreaseAsync(
        ApplyPriceIncreaseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Percent == 0)
        {
            return ApiResponse<ApplyPriceIncreaseResultDto>.Fail("Zam oranı 0 olamaz.");
        }

        var scope = (request.Scope ?? "all").Trim().ToLowerInvariant();
        IQueryable<Product> query = _db.Products;

        switch (scope)
        {
            case "all":
                break;
            case "brand":
                if (string.IsNullOrWhiteSpace(request.Brand))
                {
                    return ApiResponse<ApplyPriceIncreaseResultDto>.Fail("Marka zorunludur.");
                }

                var brand = request.Brand.Trim();
                query = query.Where(x => x.Brand.ToLower() == brand.ToLower());
                break;
            case "product":
                if (request.ProductId is null || request.ProductId == Guid.Empty)
                {
                    return ApiResponse<ApplyPriceIncreaseResultDto>.Fail("Ürün seçilmelidir.");
                }

                query = query.Where(x => x.Id == request.ProductId);
                break;
            default:
                return ApiResponse<ApplyPriceIncreaseResultDto>.Fail("Geçersiz zam kapsamı.");
        }

        var products = await query.ToListAsync(cancellationToken);
        if (products.Count == 0)
        {
            return ApiResponse<ApplyPriceIncreaseResultDto>.Fail(
                scope == "product" ? "Ürün bulunamadı." :
                scope == "brand" ? "Markaya ait ürün bulunamadı." :
                "Güncellenecek ürün bulunamadı.");
        }

        var factor = 1 + (request.Percent / 100m);
        var utcNow = DateTime.UtcNow;
        var updated = 0;
        var skipped = 0;

        foreach (var product in products)
        {
            if (product.Price is null)
            {
                skipped++;
                continue;
            }

            var next = Math.Round(product.Price.Value * factor, 2, MidpointRounding.AwayFromZero);
            if (next < 0)
            {
                next = 0;
            }

            if (next == product.Price.Value)
            {
                skipped++;
                continue;
            }

            product.Price = next;
            product.LastPriceDate = utcNow;
            product.UpdatedAt = utcNow;
            updated++;
        }

        if (updated == 0)
        {
            return ApiResponse<ApplyPriceIncreaseResultDto>.Fail(
                "Fiyatı güncellenecek ürün bulunamadı. Fiyatı olmayan ürünler atlanır.");
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<ApplyPriceIncreaseResultDto>.Ok(
            new ApplyPriceIncreaseResultDto
            {
                UpdatedCount = updated,
                SkippedCount = skipped
            },
            $"{updated} ürünün fiyatı güncellendi.");
    }

    public async Task<ApiResponse> ReorderBrandsAsync(ReorderBrandsRequest request, CancellationToken cancellationToken = default)
    {
        var brands = request.Brands
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Select(b => b.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (brands.Count < 2)
        {
            return ApiResponse.Fail("Sıralama için en az iki marka gerekir.");
        }

        await EnsureBrandOrdersAsync(brands, cancellationToken);

        var allOrders = await _db.BrandOrders
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Brand)
            .ToListAsync(cancellationToken);

        var movingSet = brands.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var indices = allOrders
            .Select((order, index) => (order, index))
            .Where(x => movingSet.Contains(x.order.Brand))
            .Select(x => x.index)
            .OrderBy(i => i)
            .ToList();

        if (indices.Count < 2)
        {
            return ApiResponse.Fail("Sıralanacak markalar bulunamadı.");
        }

        var movingInNewOrder = brands
            .Select(brand => allOrders.FirstOrDefault(o =>
                o.Brand.Equals(brand, StringComparison.OrdinalIgnoreCase)))
            .Where(o => o is not null)
            .Cast<BrandOrder>()
            .ToList();

        if (movingInNewOrder.Count != indices.Count)
        {
            return ApiResponse.Fail("Sıralama listesi geçersiz.");
        }

        var result = allOrders.ToList();
        for (var i = 0; i < indices.Count; i++)
        {
            result[indices[i]] = movingInNewOrder[i];
        }

        var utcNow = DateTime.UtcNow;
        for (var i = 0; i < result.Count; i++)
        {
            result[i].SortOrder = i;
            result[i].UpdatedAt = utcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse.Ok("Marka sırası güncellendi.");
    }

    private async Task EnsureBrandOrdersAsync(IEnumerable<string> brands, CancellationToken cancellationToken)
    {
        var brandList = brands
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Select(b => b.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (brandList.Count == 0)
        {
            return;
        }

        var existing = await _db.BrandOrders
            .Select(x => x.Brand)
            .ToListAsync(cancellationToken);

        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = brandList.Where(b => !existingSet.Contains(b)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var maxSort = existing.Count == 0
            ? -1
            : await _db.BrandOrders.MaxAsync(x => (int?)x.SortOrder, cancellationToken) ?? -1;

        foreach (var brand in missing)
        {
            maxSort++;
            _db.BrandOrders.Add(new BrandOrder
            {
                Brand = brand,
                SortOrder = maxSort
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task CleanupBrandOrderIfUnusedAsync(string brand, CancellationToken cancellationToken)
    {
        var stillUsed = await _db.Products.AnyAsync(
            x => x.Brand.ToLower() == brand.ToLower(),
            cancellationToken);

        if (stillUsed)
        {
            return;
        }

        var brandOrder = await _db.BrandOrders
            .FirstOrDefaultAsync(x => x.Brand.ToLower() == brand.ToLower(), cancellationToken);

        if (brandOrder is null)
        {
            return;
        }

        brandOrder.DeletedAt = DateTime.UtcNow;
        brandOrder.UpdatedAt = DateTime.UtcNow;
    }

    private static ProductDto Map(Product entity) => new()
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
