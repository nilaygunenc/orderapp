using OrderApp.API.Application.Exceptions;
using OrderApp.API.Application.Services;
using OrderApp.Tests.Helpers;
using Xunit;

namespace OrderApp.Tests.Services;

/// <summary>
/// ProductService testleri — cache davranışı ve ürün arama senaryoları.
/// </summary>
public class ProductServiceTests
{
    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsCorrectProduct()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        await SeedData.SeedProductsAsync(db);

        var cache = TestDbContextFactory.CreateCache();
        var service = new ProductService(db, cache);

        // Act
        var result = await service.GetByIdAsync(1);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("KB-001", result.Sku);
        Assert.Equal("Mekanik Klavye", result.Name);
        Assert.Equal(1000m, result.UnitPrice);
        Assert.Equal(10, result.StockQuantity);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var cache = TestDbContextFactory.CreateCache();
        var service = new ProductService(db, cache);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetByIdAsync_SecondCall_ReturnsCachedResult()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        await SeedData.SeedProductsAsync(db);

        var cache = TestDbContextFactory.CreateCache();
        var service = new ProductService(db, cache);

        // Act — ilk çağrı DB'den okur, cache'e yazar
        var first = await service.GetByIdAsync(1);

        // Cache'e yazıldı mı?
        Assert.True(cache.TryGetValue("products:detail:1", out _));

        // İkinci çağrı cache'den gelmeli
        var second = await service.GetByIdAsync(1);

        // Assert — aynı değerler dönmeli
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.StockQuantity, second.StockQuantity);
    }

    [Fact]
    public async Task GetAllAsync_WithSearchTerm_ReturnsMatchingProducts()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        await SeedData.SeedProductsAsync(db);

        var cache = TestDbContextFactory.CreateCache();
        var service = new ProductService(db, cache);

        // Act — isme göre ara
        var byName = await service.GetAllAsync("klavye");
        // Act — SKU'ya göre ara
        var bySku = await service.GetAllAsync("MS-002");

        // Assert
        Assert.Single(byName);
        Assert.Equal("Mekanik Klavye", byName.First().Name);

        Assert.Single(bySku);
        Assert.Equal("Kablosuz Mouse", bySku.First().Name);
    }

    [Fact]
    public async Task GetAllAsync_WithNoSearchTerm_ReturnsAllProducts()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        await SeedData.SeedProductsAsync(db);

        var cache = TestDbContextFactory.CreateCache();
        var service = new ProductService(db, cache);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }
}
