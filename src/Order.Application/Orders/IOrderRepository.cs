using Order.Application.Orders.Models;
using Order.Application.Orders.Requests;

namespace Order.Application.Orders;

public interface IOrderRepository
{
    Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderSummary>> GetOrdersByStatusIdAsync(Guid statusId, CancellationToken cancellationToken = default);

    Task<OrderDetail?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<Guid?> GetOrderStatusIdAsync(string statusName, CancellationToken cancellationToken = default);

    Task<bool> UpdateOrderStatusAsync(Guid orderId, Guid statusId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>> GetExistingProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default);

    Task<Guid> CreateOrderAsync(CreateOrderRequest request, Guid statusId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyProfit>> GetMonthlyProfitAsync(CancellationToken cancellationToken = default);
}
