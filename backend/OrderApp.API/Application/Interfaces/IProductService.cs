using OrderApp.API.Application.DTOs;

namespace OrderApp.API.Application.Interfaces;

public interface IProductService
{
    /// <summary>
    /// Tüm ürünleri döner. search parametresi varsa isim veya SKU'ya göre filtreler.
    /// Sonuçlar IMemoryCache üzerinden sunulur.
    /// </summary>
    Task<IEnumerable<ProductResponseDto>> GetAllAsync(string? search = null);

    /// <summary>
    /// Tek ürün döner. Bulunamazsa NotFoundException fırlatır.
    /// Sonuç IMemoryCache üzerinden sunulur.
    /// </summary>
    Task<ProductResponseDto> GetByIdAsync(int id);
}
