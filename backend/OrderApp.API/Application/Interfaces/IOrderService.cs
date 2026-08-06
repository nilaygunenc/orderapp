using OrderApp.API.Application.DTOs;

namespace OrderApp.API.Application.Interfaces;

public interface IOrderService
{
    /// <summary>
    /// Yeni sipariş oluşturur.
    /// Stok kontrolü, transaction yönetimi ve cache invalidation bu metot içinde yapılır.
    /// </summary>
    Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto);

    /// <summary>Tüm siparişleri özet bilgiyle döner.</summary>
    Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync();

    /// <summary>
    /// Tek sipariş detayını döner. Bulunamazsa NotFoundException fırlatır.
    /// </summary>
    Task<OrderResponseDto> GetOrderByIdAsync(int id);
}
