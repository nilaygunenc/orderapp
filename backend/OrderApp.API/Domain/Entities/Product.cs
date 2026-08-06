namespace OrderApp.API.Domain.Entities;

public class Product
{
    public int Id { get; set; }

    /// <summary>Benzersiz stok kodu</summary>
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Negatif olamaz</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Mevcut stok miktarı, negatif olamaz</summary>
    public int StockQuantity { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
