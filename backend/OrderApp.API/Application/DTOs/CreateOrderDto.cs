using System.ComponentModel.DataAnnotations;

namespace OrderApp.API.Application.DTOs;

public class CreateOrderDto
{
    [Required(ErrorMessage = "Müşteri adı zorunludur.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Müşteri adı 2-200 karakter arasında olmalıdır.")]
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// En az bir ürün kalemi olmalıdır.
    /// Bireysel item validation'ı CreateOrderItemDto üzerinde yapılır.
    /// </summary>
    [Required(ErrorMessage = "Sipariş kalemleri zorunludur.")]
    [MinLength(1, ErrorMessage = "Siparişte en az bir ürün bulunmalıdır.")]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}
