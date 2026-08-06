using Microsoft.EntityFrameworkCore;
using OrderApp.API.Domain.Entities;

namespace OrderApp.API.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureProduct(modelBuilder);
        ConfigureOrder(modelBuilder);
        ConfigureOrderItem(modelBuilder);
        SeedProducts(modelBuilder);
    }

    // ------------------------------------------------------------------ //
    //  Fluent API konfigürasyonları
    // ------------------------------------------------------------------ //

    private static void ConfigureProduct(ModelBuilder mb)
    {
        mb.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);

            e.Property(p => p.Sku)
                .IsRequired()
                .HasMaxLength(50);

            // SKU benzersiz olmalı
            e.HasIndex(p => p.Sku)
                .IsUnique();

            e.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            // Para değerleri için 18,2 hassasiyeti
            e.Property(p => p.UnitPrice)
                .HasColumnType("decimal(18,2)");

            // Negatif olamaz kısıtı (DB seviyesinde)
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Product_UnitPrice", "\"UnitPrice\" >= 0");
                t.HasCheckConstraint("CK_Product_StockQuantity", "\"StockQuantity\" >= 0");
            });
        });
    }

    private static void ConfigureOrder(ModelBuilder mb)
    {
        mb.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);

            e.Property(o => o.CustomerName)
                .IsRequired()
                .HasMaxLength(200);

            e.Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            e.Property(o => o.OrderDate)
                .IsRequired();

            // Sıralama sorgularını hızlandırır (GetAllOrdersAsync OrderByDescending kullanır)
            e.HasIndex(o => o.OrderDate);
        });
    }

    private static void ConfigureOrderItem(ModelBuilder mb)
    {
        mb.Entity<OrderItem>(e =>
        {
            e.HasKey(oi => oi.Id);

            e.Property(oi => oi.UnitPrice)
                .HasColumnType("decimal(18,2)");

            e.HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // JOIN sorgularını hızlandırır — SQLite/PostgreSQL FK için otomatik index üretmez
            e.HasIndex(oi => oi.OrderId);
            e.HasIndex(oi => oi.ProductId);

            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_OrderItem_Quantity",  "\"Quantity\" > 0");
                t.HasCheckConstraint("CK_OrderItem_UnitPrice", "\"UnitPrice\" >= 0");
            });
        });
    }

    // ------------------------------------------------------------------ //
    //  Seed Data — en az 5 örnek ürün
    // ------------------------------------------------------------------ //

    private static void SeedProducts(ModelBuilder mb)
    {
        mb.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Sku = "KB-001",
                Name = "Mekanik Klavye",
                UnitPrice = 1299.99m,
                StockQuantity = 50
            },
            new Product
            {
                Id = 2,
                Sku = "MS-002",
                Name = "Kablosuz Mouse",
                UnitPrice = 449.90m,
                StockQuantity = 80
            },
            new Product
            {
                Id = 3,
                Sku = "MN-003",
                Name = "27\" IPS Monitör",
                UnitPrice = 5499.00m,
                StockQuantity = 20
            },
            new Product
            {
                Id = 4,
                Sku = "HP-004",
                Name = "Kulaklık (Noise Cancelling)",
                UnitPrice = 2199.50m,
                StockQuantity = 35
            },
            new Product
            {
                Id = 5,
                Sku = "WC-005",
                Name = "1080p Web Kamerası",
                UnitPrice = 799.00m,
                StockQuantity = 60
            }
        );
    }
}
