using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using OrderApp.API.Application.Common;
using OrderApp.API.Application.DTOs;
using OrderApp.API.Application.Exceptions;
using OrderApp.API.Application.Interfaces;
using OrderApp.API.Infrastructure.Persistence;

namespace OrderApp.API.Application.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public ProductService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    // ------------------------------------------------------------------ //

    public async Task<IEnumerable<ProductResponseDto>> GetAllAsync(string? search = null)
    {
        // Arama varsa farklı bir cache key kullan; yoksa genel listeyi döndür
        var normalizedSearch = search?.Trim();
        var cacheKey = string.IsNullOrWhiteSpace(normalizedSearch)
            ? CacheKeys.ProductList
            : CacheKeys.ProductSearch(normalizedSearch);

        if (_cache.TryGetValue(cacheKey, out IEnumerable<ProductResponseDto>? cached) && cached is not null)
            return cached;

        var query = _db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            // Trim uygula, sonra DB-native LIKE kullan — index dostu, ToLower() kaçınılır
            var term = $"%{normalizedSearch}%";
            query = query.Where(p =>
                EF.Functions.Like(p.Name, term) ||
                EF.Functions.Like(p.Sku,  term));
        }

        var products = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductResponseDto(p.Id, p.Sku, p.Name, p.UnitPrice, p.StockQuantity))
            .ToListAsync();

        _cache.Set(cacheKey, products, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheKeys.DefaultExpiration,
            Size = Math.Max(1, products.Count)
        }.AddExpirationToken(
            new CancellationChangeToken(CacheKeys.ProductExpirationToken)));

        return products;
    }

    // ------------------------------------------------------------------ //

    public async Task<ProductResponseDto> GetByIdAsync(int id)
    {
        var cacheKey = CacheKeys.ProductDetail(id);

        if (_cache.TryGetValue(cacheKey, out ProductResponseDto? cached) && cached is not null)
            return cached;

        var product = await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductResponseDto(p.Id, p.Sku, p.Name, p.UnitPrice, p.StockQuantity))
            .FirstOrDefaultAsync();

        if (product is null)
            throw new NotFoundException("Ürün", id);

        _cache.Set(cacheKey, product, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheKeys.DefaultExpiration,
            Size = 1
        }.AddExpirationToken(
            new CancellationChangeToken(CacheKeys.ProductExpirationToken)));

        return product;
    }
}
