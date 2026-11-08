using Order.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Order.Data;

public interface IOrderRepository
{
    Task<IEnumerable<OrderSummary>> GetOrdersAsync();

    Task<OrderDetail> GetOrderByIdAsync(Guid orderId);

    Task<IEnumerable<OrderSummary>> GetOrdersByStatusIdAsync(Guid statusId);

    Task<Guid?> GetOrderStatusIdAsync(string statusName);

    Task<bool> UpdateOrderStatusAsync(Guid orderId, Guid statusId);

    Task<bool> ProductExistsAsync(Guid productId);

    Task<Guid> CreateOrderAsync(CreateOrderRequest request);

    Task<IEnumerable<MonthlyProfit>> GetMonthlyProfitAsync();
}
