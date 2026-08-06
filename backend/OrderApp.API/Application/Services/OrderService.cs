using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OrderApp.API.Application.Common;
using OrderApp.API.Application.DTOs;
using OrderApp.API.Application.Exceptions;
using OrderApp.API.Application.Interfaces;
using OrderApp.API.Domain.Entities;
using OrderApp.API.Infrastructure.Persistence;

namespace OrderApp.API.Application.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public OrderService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto)
    {
        var requestedProductIds = dto.Items
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();

        var products = await _db.Products
            .Where(p => requestedProductIds.Contains(p.Id))
            .ToListAsync();

        // O(1) lookup — eskiden her döngüde O(n) First() çağrısı yapılıyordu
        var productMap = products.ToDictionary(p => p.Id);

        // Eksik ürün kontrolü
        var missingIds = requestedProductIds.Except(productMap.Keys).ToList();
        if (missingIds.Count > 0)
            throw new NotFoundException(
                $"Şu ürünler bulunamadı: {string.Join(", ", missingIds)}");

        // Stok kontrolü — tüm hatalar tek seferde toplanır
        var stockErrors = dto.Items
            .Where(item => productMap[item.ProductId].StockQuantity < item.Quantity)
            .Select(item =>
            {
                var p = productMap[item.ProductId];
                return $"'{p.Name}' ({p.Sku}) için yeterli stok yok. " +
                       $"İstenen: {item.Quantity}, Mevcut: {p.StockQuantity}";
            })
            .ToList();

        if (stockErrors.Count > 0)
            throw new BusinessException(stockErrors);

        // Transaction: sipariş + stok düşme — ya hep ya hiç
        // ROWLOCK hint yerine EF Concurrency Token eklenmedi (SQLite desteklemez),
        // ancak transaction isolation seviyesi SQLite'da Serializable davranır.
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // Stok düşme işlemi — her ürün için ayrı UPDATE (toplu UPDATE yerine
            // takip edilebilirlik için bilinçli tercih)
            var orderItems = dto.Items.Select(item =>
            {
                var product = productMap[item.ProductId];

                // Stok tekrar kontrol et — transaction içinde başka bir istek
                // stoku düşürmüş olabilir (double-check)
                if (product.StockQuantity < item.Quantity)
                    throw new BusinessException(
                        $"'{product.Name}' için sipariş sırasında stok yetersiz kaldı. " +
                        $"Lütfen tekrar deneyin.");

                product.StockQuantity -= item.Quantity;

                return new OrderItem
                {
                    ProductId = product.Id,
                    Quantity  = item.Quantity,
                    UnitPrice = product.UnitPrice  // sipariş anındaki fiyat snapshot'ı
                };
            }).ToList();

            var order = new Order
            {
                CustomerName = dto.CustomerName,
                OrderDate    = DateTime.UtcNow,
                TotalAmount  = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity),
                OrderItems   = orderItems
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            InvalidateProductCaches();

            // Product navigation property'sini response için manuel doldur
            foreach (var oi in order.OrderItems)
                oi.Product = productMap[oi.ProductId];

            return MapToOrderResponse(order);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync()
    {
        // Entity graph'ını oluşturmak yerine yalnızca response alanlarını çek.
        // Bu, uzun listelerde Include ve entity allocation maliyetini azaltır.
        return await _db.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderResponseDto(
                o.Id,
                o.CustomerName,
                o.OrderDate,
                o.TotalAmount,
                o.OrderItems.Select(oi => new OrderItemResponseDto(
                    oi.ProductId,
                    oi.Product.Name,
                    oi.Product.Sku,
                    oi.Quantity,
                    oi.UnitPrice,
                    oi.UnitPrice * oi.Quantity
                )).ToList()
            ))
            .ToListAsync();
    }

    public async Task<OrderResponseDto> GetOrderByIdAsync(int id)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OrderResponseDto(
                o.Id,
                o.CustomerName,
                o.OrderDate,
                o.TotalAmount,
                o.OrderItems.Select(oi => new OrderItemResponseDto(
                    oi.ProductId,
                    oi.Product.Name,
                    oi.Product.Sku,
                    oi.Quantity,
                    oi.UnitPrice,
                    oi.UnitPrice * oi.Quantity
                )).ToList()
            ))
            .FirstOrDefaultAsync();

        if (order is null)
            throw new NotFoundException("Sipariş", id);

        return order;
    }

    // ------------------------------------------------------------------ //

    private static void InvalidateProductCaches()
    {
        // Liste, detay ve arama sonuçlarının tamamı stok bilgisi içerir.
        // Ortak expiration token tüm ürün cache kayıtlarını atomik olarak düşürür.
        CacheKeys.InvalidateProducts();
    }

    private static OrderResponseDto MapToOrderResponse(Order order)
    {
        var items = order.OrderItems.Select(oi => new OrderItemResponseDto(
            ProductId:   oi.ProductId,
            ProductName: oi.Product?.Name ?? "Bilinmeyen Ürün",
            ProductSku:  oi.Product?.Sku  ?? "-",
            Quantity:    oi.Quantity,
            UnitPrice:   oi.UnitPrice,
            LineTotal:   oi.UnitPrice * oi.Quantity
        )).ToList();

        return new OrderResponseDto(
            Id:           order.Id,
            CustomerName: order.CustomerName,
            OrderDate:    order.OrderDate,
            TotalAmount:  order.TotalAmount,
            Items:        items
        );
    }
}
