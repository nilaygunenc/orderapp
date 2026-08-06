using Microsoft.AspNetCore.Mvc;
using OrderApp.API.Application.DTOs;
using OrderApp.API.Application.Interfaces;

namespace OrderApp.API.Controllers;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Tüm siparişleri listeler.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetAll()
    {
        var orders = await _orderService.GetAllOrdersAsync();
        return Ok(orders);
    }

    /// <summary>
    /// Belirtilen Id'ye sahip siparişin detayını döner.
    /// </summary>
    /// <param name="id">Sipariş Id</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponseDto>> GetById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        return Ok(order);
    }

    /// <summary>
    /// Yeni sipariş oluşturur.
    /// Stok yetersizse 422, ürün bulunamazsa 404, validation hatasıysa 400 döner.
    /// </summary>
    /// <param name="dto">Sipariş bilgileri</param>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponseDto>> Create([FromBody] CreateOrderDto dto)
    {
        var order = await _orderService.CreateOrderAsync(dto);

        // 201 Created + Location header: GET /api/orders/{id}
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }
}
