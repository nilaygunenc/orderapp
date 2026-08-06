namespace OrderApp.API.Application.DTOs;

public record OrderResponseDto(
    int Id,
    string CustomerName,
    DateTime OrderDate,
    decimal TotalAmount,
    List<OrderItemResponseDto> Items
);

/// <param name="ProductId">Ürün kimliği.</param>
/// <param name="ProductName">Ürün adı.</param>
/// <param name="ProductSku">Benzersiz stok kodu.</param>
/// <param name="Quantity">Sipariş miktarı.</param>
/// <param name="UnitPrice">Sipariş anındaki birim fiyat.</param>
/// <param name="LineTotal">Kalemin toplam tutarı.</param>
public record OrderItemResponseDto(
    int ProductId,
    string ProductName,
    string ProductSku,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);
