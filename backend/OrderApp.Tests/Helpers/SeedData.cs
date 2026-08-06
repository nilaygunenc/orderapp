using OrderApp.API.Domain.Entities;
using OrderApp.API.Infrastructure.Persistence;

namespace OrderApp.Tests.Helpers;

/// <summary>
/// Testlerde kullanılacak örnek verileri DbContext'e yükler.
/// </summary>
public static class SeedData
{
    /// <summary>
    /// İki adet test ürünü ekler:
    ///   - Id=1  "Mekanik Klavye"  fiyat:1000  stok:10
    ///   - Id=2  "Kablosuz Mouse"  fiyat:500   stok:5
    /// </summary>
    public static async Task SeedProductsAsync(AppDbContext db)
    {
        db.Products.AddRange(
            new Product
            {
                Id = 1,
                Sku = "KB-001",
                Name = "Mekanik Klavye",
                UnitPrice = 1000m,
                StockQuantity = 10
            },
            new Product
            {
                Id = 2,
                Sku = "MS-002",
                Name = "Kablosuz Mouse",
                UnitPrice = 500m,
                StockQuantity = 5
            }
        );

        await db.SaveChangesAsync();
    }
}
