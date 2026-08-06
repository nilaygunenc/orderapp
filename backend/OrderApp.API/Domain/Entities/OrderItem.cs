namespace OrderApp.API.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>Kaç adet sipariş edildi</summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Sipariş anındaki birim fiyat.
    /// Product.UnitPrice sonradan değişse bile bu alan sabit kalır.
    /// </summary>
    public decimal UnitPrice { get; set; }
}
