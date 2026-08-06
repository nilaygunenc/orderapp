using System.ComponentModel.DataAnnotations;

namespace OrderApp.API.Application.DTOs;

public class CreateOrderItemDto
{
    [Required(ErrorMessage = "Ürün Id zorunludur.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir ürün Id giriniz.")]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Sipariş miktarı sıfırdan büyük olmalıdır.")]
    public int Quantity { get; set; }
}
