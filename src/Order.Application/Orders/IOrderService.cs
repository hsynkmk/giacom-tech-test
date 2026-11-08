using Order.Application.Common;
using Order.Application.Orders.Models;
using Order.Application.Orders.Requests;

namespace Order.Application.Orders;

public interface IOrderService
{
    Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(CancellationToken cancellationToken = default);

    Task<ServiceResult<OrderDetail>> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<OrderSummary>>> GetOrdersByStatusAsync(string status, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<OrderDetail>> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyProfit>> GetMonthlyProfitAsync(CancellationToken cancellationToken = default);
}
