using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Order.Model;
using Order.Service;
using System;
using System.Threading.Tasks;

namespace OrderService.WebAPI.Controllers;

[ApiController]
[Route("orders")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get()
    {
        var orders = await _orderService.GetOrdersAsync();
        return Ok(orders);
    }

    [HttpGet("{orderId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid orderId)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order != null)
        {
            return Ok(order);
        }
        else
        {
            return NotFound();
        }
    }

    [HttpGet("status/{status}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOrdersByStatus(string status)
    {
        var orders = await _orderService.GetOrdersByStatusAsync(status);
        if (orders == null)
        {
            return BadRequest($"Order status '{status}' does not exist!");
        }

        return Ok(orders);
    }

    [HttpPut("{orderId}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrderStatus(Guid orderId, UpdateOrderStatusRequest request)
    {
        var result = await _orderService.UpdateOrderStatusAsync(orderId, request.Status);

        switch (result)
        {
            case UpdateOrderStatusResult.InvalidStatus:
                return BadRequest($"Order status '{request.Status}' does not exist.");
            case UpdateOrderStatusResult.OrderNotFound:
                return NotFound();
            default:
                return NoContent();
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        var errors = await _orderService.ValidateCreateOrderAsync(request);
        foreach (var error in errors)
        {
            ModelState.AddModelError(nameof(request), error);
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // I return 201 with a Location header pointing at GET /orders/{id}.
        var order = await _orderService.CreateOrderAsync(request);
        return CreatedAtAction(nameof(GetOrderById), new { orderId = order.Id }, order);
    }


    [HttpGet("profit/monthly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonthlyProfit()
    {
        var profits = await _orderService.GetMonthlyProfitAsync();
        return Ok(profits);
    }
}
