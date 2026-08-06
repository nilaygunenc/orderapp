namespace OrderApp.API.Domain.Entities;

public class Order
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Sipariş anındaki fiyatlardan hesaplanan toplam tutar.
    /// Sonraki fiyat değişikliklerinden etkilenmez.
    /// </summary>
    public decimal TotalAmount { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
