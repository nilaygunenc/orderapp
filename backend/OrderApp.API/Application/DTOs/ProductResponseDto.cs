namespace OrderApp.API.Application.DTOs;

public record ProductResponseDto(
    int Id,
    string Sku,
    string Name,
    decimal UnitPrice,
    int StockQuantity
);
