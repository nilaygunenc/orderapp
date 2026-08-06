using OrderApp.API.Application.DTOs;
using OrderApp.API.Application.Exceptions;
using OrderApp.API.Application.Services;
using OrderApp.Tests.Helpers;
using Xunit;

namespace OrderApp.Tests.Services;

/// <summary>
/// OrderService'in kritik business senaryolarını test eder.
/// Her test kendi InMemory veritabanında izole çalışır.
/// </summary>
public class OrderServiceTests
{
    // ================================================================== //
    //  TEST 1 — Yetersiz stok durumunda sipariş oluşturulmamalı
    // ================================================================== //

    [Fact]
    public async Task CreateOrderAsync_WhenStockIsInsufficient_ThrowsBusinessException()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        await SeedData.SeedProductsAsync(db);
        // Mekanik Klavye stoğu = 10, biz 99 adet istiyoruz

        var cache = TestDbContextFactory.CreateCache();
        var service = new OrderService(db, cache);

        var dto = new CreateOrderDto
        {
            CustomerName = "Test Müşteri",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 99 }   // stok 10, istek 99
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.CreateOrderAsync(dto));

        // Hata mesajı Errors listesinde ürün adını içermeli
        Assert.Contains(exception.Errors, e => e.Contains("Mekanik Klavye"));
    }

    [Fact]
    public async Task CreateOrderAsync_WhenStockIsInsufficient_DoesNotReduceAnyStock()
    {
        // Arrange — iki üründen biri yeterli stokta, diğeri değil
        await using var db = TestDbContextFactory.Create();
        await SeedData.SeedProductsAsync(db);
        // Klavye stok:10 → 5 adet OK
        // Mouse stok:5  → 99 adet YETERSIZ

        var cache = TestDbContextFactory.CreateCache();
        var service = new OrderService(db, cache);

        var dto = new CreateOrderDto
        {
            CustomerName = "Test Müşteri",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 5  },  // yeterli
                new() { ProductId = 2, Quantity = 99 }   // yetersiz
            }
        };

        // Act — exception bekleniyor
        await Assert.ThrowsAsync<BusinessException>(
            () => service.CreateOrderAsync(dto));

        // Assert — yetersiz stok olduğunda hiçbir ürünün stoğu değişmemeli
        var keyboard = await db.Products.FindAsync(1);
        var mouse = await db.Products.FindAsync(2);

        Assert.Equal(10, keyboard!.StockQuantity);   // değişmemiş olmalı
        Assert.Equal(5, mouse!.StockQuantity);        // değişmemiş olmalı
    }

    [Fact]
    public async Task CreateOrderAsync_WhenMultipleItemsInsufficientStock_ReturnsAllErrorsInException()
    {
        // Arrange — iki ürün de yetersiz stokta
        await using var db = TestDbContextFactory.Create();
        await SeedData.SeedProductsAsync(db);

        var cache = TestDbContextFactory.CreateCache();
        var service = new OrderService(db, cache);

        var dto = new CreateOrderDto
        {
            CustomerName = "Test Müşteri",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 999 },  // stok 10
                new() { ProductId = 2, Quantity = 999 }   // stok 5
            }
        };

        // Act
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.CreateOrderAsync(dto));

        // Assert — her iki ürünün hatası da dönmeli (sadece ilki değil)
        Assert.Equal(2, exception.Errors.Count);
    }

    // ================================================================== //
    //  TEST 2 — Başarılı sipariş sonrası stoklar doğru azalmalı
    // ================================================================== //

    [Fact]
    public async Task CreateOrderAsync_WhenStockIsSufficient_ReducesStockByOrderedQuantity()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        await SeedData.SeedProductsAsync(db);
        // Klavye stok:10, Mouse stok:5

        var cache = TestDbContextFactory.CreateCache();
        var service = new OrderService(db, cache);

        var dto = new CreateOrderDto
        {
            CustomerName = "Ali Yılmaz",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 3 },   // 10 - 3 = 7
                new() { ProductId = 2, Quantity = 2 }    //  5 - 2 = 3
            }
        };

        // Act
        var result = await service.CreateOrderAsync(dto);

        // Assert — stoklar sipariş miktarı kadar düşmüş olmalı
        var keyboard = await db.Products.FindAsync(1);
        var mouse = await db.Products.FindAsync(2);

        Assert.Equal(7, keyboard!.StockQuantity);
        Assert.Equal(3, mouse!.StockQuantity);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenStockIsSufficient_SavesOrderWithCorrectTotalAmount()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        await SeedData.SeedProductsAsync(db);
        // Klavye fiyat:1000, Mouse fiyat:500

        var cache = TestDbContextFactory.CreateCache();
        var service = new OrderService(db, cache);

        var dto = new CreateOrderDto
        {
            CustomerName = "Ayşe Kara",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 2 },   // 2 x 1000 = 2000
                new() { ProductId = 2, Quantity = 4 }    // 4 x  500 = 2000
                                                          // Toplam   = 4000
            }
        };

        // Act
        var result = await service.CreateOrderAsync(dto);

        // Assert
        Assert.Equal(4000m, result.TotalAmount);
        Assert.Equal("Ayşe Kara", result.CustomerName);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenStockIsSufficient_SnapshotsUnitPriceAtOrderTime()
    {
        // Arrange
        // Bu test, sipariş sonrası ürün fiyatı değişse bile
        // OrderItem.UnitPrice'ın orijinal değeri koruduğunu doğrular.
        await using var db = TestDbContextFactory.Create();
        await SeedData.SeedProductsAsync(db);

        var cache = TestDbContextFactory.CreateCache();
        var service = new OrderService(db, cache);

        var dto = new CreateOrderDto
        {
            CustomerName = "Mehmet Demir",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 1 }  // fiyat şu an: 1000
            }
        };

        // Act — siparişi oluştur
        var result = await service.CreateOrderAsync(dto);

        // Sonra ürün fiyatını değiştir
        var product = await db.Products.FindAsync(1);
        product!.UnitPrice = 9999m;
        await db.SaveChangesAsync();

        // Assert — siparişteki birim fiyat hâlâ 1000 olmalı
        var savedItem = result.Items.First(i => i.ProductId == 1);
        Assert.Equal(1000m, savedItem.UnitPrice);
    }

    // ================================================================== //
    //  TEST 3 — Edge case'ler
    // ================================================================== //

    [Fact]
    public async Task CreateOrderAsync_WhenProductDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        await SeedData.SeedProductsAsync(db);

        var cache = TestDbContextFactory.CreateCache();
        var service = new OrderService(db, cache);

        var dto = new CreateOrderDto
        {
            CustomerName = "Test",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = 9999, Quantity = 1 }  // var olmayan Id
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.CreateOrderAsync(dto));
    }

    [Fact]
    public async Task CreateOrderAsync_WhenSuccessful_InvalidatesProductCache()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        await SeedData.SeedProductsAsync(db);

        var cache = TestDbContextFactory.CreateCache();
        var productService = new ProductService(db, cache);
        var orderService = new OrderService(db, cache);

        // Önce cache'e at
        await productService.GetByIdAsync(1);
        Assert.True(cache.TryGetValue("products:detail:1", out _), "Cache dolu olmalı");

        var dto = new CreateOrderDto
        {
            CustomerName = "Cache Test",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        // Act — sipariş oluştur (cache invalidation tetiklenmeli)
        await orderService.CreateOrderAsync(dto);

        // Assert — cache temizlenmiş olmalı
        Assert.False(cache.TryGetValue("products:detail:1", out _), "Sipariş sonrası cache temizlenmiş olmalı");
    }

    [Fact]
    public async Task CreateOrderAsync_WhenSuccessful_InvalidatesSearchCache()
    {
        await using var db = TestDbContextFactory.Create();
        await SeedData.SeedProductsAsync(db);

        var cache = TestDbContextFactory.CreateCache();
        var productService = new ProductService(db, cache);
        var orderService = new OrderService(db, cache);

        await productService.GetAllAsync("klavye");
        Assert.True(cache.TryGetValue("products:search:klavye", out _));

        await orderService.CreateOrderAsync(new CreateOrderDto
        {
            CustomerName = "Arama Cache Test",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        });

        Assert.False(cache.TryGetValue("products:search:klavye", out _));
    }
}
