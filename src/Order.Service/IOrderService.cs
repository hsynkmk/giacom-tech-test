using Order.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Order.Service;

public interface IOrderService
{
    Task<IEnumerable<OrderSummary>> GetOrdersAsync();
    
    Task<OrderDetail> GetOrderByIdAsync(Guid orderId);

    Task<IEnumerable<OrderSummary>> GetOrdersByStatusAsync(string status);

    Task<UpdateOrderStatusResult> UpdateOrderStatusAsync(Guid orderId, string status);

    Task<IEnumerable<string>> ValidateCreateOrderAsync(CreateOrderRequest request);

    Task<OrderDetail> CreateOrderAsync(CreateOrderRequest request);

    Task<IEnumerable<MonthlyProfit>> GetMonthlyProfitAsync();

}
