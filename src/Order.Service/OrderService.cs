using Order.Data;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Order.Service;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;

    public OrderService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IEnumerable<OrderSummary>> GetOrdersAsync()
    {
        var orders = await _orderRepository.GetOrdersAsync();
        return orders;
    }

    public async Task<OrderDetail> GetOrderByIdAsync(Guid orderId)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId);
        return order;
    }

    // null for an unknown status instead of an empty list, so the controller can return a 400
    // otherwise a typo like "Faild" would will be like "no orders" or something
    public async Task<IEnumerable<OrderSummary>> GetOrdersByStatusAsync(string status)
    {
        var statusId = await _orderRepository.GetOrderStatusIdAsync(status);
        if (statusId == null)
        {
            return null;
        }

        var orders = await _orderRepository.GetOrdersByStatusIdAsync(statusId.Value);
        return orders;
    }

    // return an enum instead of a bool. That lets the controller tell them apart
    public async Task<UpdateOrderStatusResult> UpdateOrderStatusAsync(Guid orderId, string status)
    {
        var statusId = await _orderRepository.GetOrderStatusIdAsync(status);
        if (statusId == null)
        {
            return UpdateOrderStatusResult.InvalidStatus;
        }

        var updated = await _orderRepository.UpdateOrderStatusAsync(orderId, statusId.Value);
        return updated ? UpdateOrderStatusResult.Updated : UpdateOrderStatusResult.OrderNotFound;
    }

    // The data annotations on CreateOrderRequest already check the shape of the request (required fields, quantity >= 1).
    // but here we check the rules that need the database
    public async Task<IEnumerable<string>> ValidateCreateOrderAsync(CreateOrderRequest request)
    {
        var errors = new List<string>();

        // [Required] only checks for null, so here we check Guid.Empty
        if (request.ResellerId == Guid.Empty)
        {
            errors.Add("ResellerId must not be empty.");
        }

        if (request.CustomerId == Guid.Empty)
        {
            errors.Add("CustomerId must not be empty.");
        }

        var productIds = request.Items
            .Where(x => x.ProductId.HasValue)
            .Select(x => x.ProductId.Value)
            .Distinct();

        foreach (var productId in productIds)
        {
            if (!await _orderRepository.ProductExistsAsync(productId))
            {
                errors.Add($"Product '{productId}' does not exist.");
            }
        }

        return errors;
    }

    public async Task<OrderDetail> CreateOrderAsync(CreateOrderRequest request)
    {
        var orderId = await _orderRepository.CreateOrderAsync(request);
        var order = await _orderRepository.GetOrderByIdAsync(orderId);
        return order;
    }

    public async Task<IEnumerable<MonthlyProfit>> GetMonthlyProfitAsync()
    {
        var profits = await _orderRepository.GetMonthlyProfitAsync();
        return profits;
    }
}
