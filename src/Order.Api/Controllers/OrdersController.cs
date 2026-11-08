using Microsoft.AspNetCore.Mvc;
using Order.Api.Extensions;
using Order.Application.Orders;
using Order.Application.Orders.Models;
using Order.Application.Orders.Requests;

namespace Order.Api.Controllers;

[ApiController]
[Route("orders")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OrderSummary>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetOrdersAsync(cancellationToken);
        return Ok(orders);
    }

    [HttpGet("{orderId}")]
    [ProducesResponseType<OrderDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await _orderService.GetOrderByIdAsync(orderId, cancellationToken);
        return result.ToActionResult(this);
    }

    // The new routes have their own segment ("status/...", "profit/...") so they can't clash with "{orderId}".
    [HttpGet("status/{status}")]
    [ProducesResponseType<IReadOnlyList<OrderSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOrdersByStatus(string status, CancellationToken cancellationToken)
    {
        var result = await _orderService.GetOrdersByStatusAsync(status, cancellationToken);
        return result.ToActionResult(this);
    }

    // PUT on a "status" sub-resource because only the status changes, not the whole order.
    [HttpPut("{orderId}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrderStatus(Guid orderId, UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _orderService.UpdateOrderStatusAsync(orderId, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [ProducesResponseType<OrderDetail>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _orderService.CreateOrderAsync(request, cancellationToken);

        // 201 with a Location header pointing at GET /orders/{id}.
        return result.ToActionResult(this, order => CreatedAtAction(nameof(GetOrderById), new { orderId = order.Id }, order));
    }

    [HttpGet("profit/monthly")]
    [ProducesResponseType<IReadOnlyList<MonthlyProfit>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonthlyProfit(CancellationToken cancellationToken)
    {
        var profits = await _orderService.GetMonthlyProfitAsync(cancellationToken);
        return Ok(profits);
    }
}
