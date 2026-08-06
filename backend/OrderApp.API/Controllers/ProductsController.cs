using Microsoft.AspNetCore.Mvc;
using OrderApp.API.Application.DTOs;
using OrderApp.API.Application.Interfaces;

namespace OrderApp.API.Controllers;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Tüm ürünleri listeler. Opsiyonel 'search' parametresiyle isim veya SKU'ya göre filtreler.
    /// </summary>
    /// <param name="search">Arama terimi (ürün adı veya stok kodu)</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetAll(
        [FromQuery] string? search = null)
    {
        var products = await _productService.GetAllAsync(search);
        return Ok(products);
    }

    /// <summary>
    /// Belirtilen Id'ye sahip ürünün detayını döner.
    /// </summary>
    /// <param name="id">Ürün Id</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponseDto>> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        return Ok(product);
    }
}
