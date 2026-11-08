using FluentValidation;
using Order.Application.Common;
using Order.Application.Orders.Models;
using Order.Application.Orders.Requests;
using Order.Domain;

namespace Order.Application.Orders;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IValidator<CreateOrderRequest> _createOrderValidator;
    private readonly IValidator<UpdateOrderStatusRequest> _updateOrderStatusValidator;

    public OrderService(
        IOrderRepository orderRepository,
        IValidator<CreateOrderRequest> createOrderValidator,
        IValidator<UpdateOrderStatusRequest> updateOrderStatusValidator)
    {
        _orderRepository = orderRepository;
        _createOrderValidator = createOrderValidator;
        _updateOrderStatusValidator = updateOrderStatusValidator;
    }

    public Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        return _orderRepository.GetOrdersAsync(cancellationToken);
    }

    public async Task<ServiceResult<OrderDetail>> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);

        return order is null
            ? ServiceResult<OrderDetail>.Failure(OrderErrors.OrderNotFound(orderId))
            : ServiceResult<OrderDetail>.Success(order);
    }

    // An unknown status is a validation error rather than an empty list,
    // otherwise a typo like "Faild" would look the same as "no orders".
    public async Task<ServiceResult<IReadOnlyList<OrderSummary>>> GetOrdersByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        var statusId = await _orderRepository.GetOrderStatusIdAsync(status, cancellationToken);
        if (statusId is null)
        {
            return ServiceResult<IReadOnlyList<OrderSummary>>.Failure(OrderErrors.UnknownStatus(status));
        }

        var orders = await _orderRepository.GetOrdersByStatusIdAsync(statusId.Value, cancellationToken);
        return ServiceResult<IReadOnlyList<OrderSummary>>.Success(orders);
    }

    public async Task<ServiceResult> UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateOrderStatusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ServiceResult.Failure(Error.Validation(validation.ToDictionary()));
        }

        var statusId = await _orderRepository.GetOrderStatusIdAsync(request.Status!, cancellationToken);
        if (statusId is null)
        {
            return ServiceResult.Failure(OrderErrors.UnknownStatus(request.Status!));
        }

        var updated = await _orderRepository.UpdateOrderStatusAsync(orderId, statusId.Value, cancellationToken);

        return updated
            ? ServiceResult.Success()
            : ServiceResult.Failure(OrderErrors.OrderNotFound(orderId));
    }

    public async Task<ServiceResult<OrderDetail>> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createOrderValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ServiceResult<OrderDetail>.Failure(Error.Validation(validation.ToDictionary()));
        }

        // Every new order starts as "Created". If that status is missing the reference data is broken,
        // which is unexpected, so this is an exception rather than a ServiceResult failure.
        var createdStatusId = await _orderRepository.GetOrderStatusIdAsync(OrderStatusNames.Created, cancellationToken)
            ?? throw new InvalidOperationException($"The '{OrderStatusNames.Created}' order status is missing from the database.");

        var orderId = await _orderRepository.CreateOrderAsync(request, createdStatusId, cancellationToken);

        // Load the order back so the response has the same shape as GET /orders/{id}.
        var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
        return ServiceResult<OrderDetail>.Success(order!);
    }

    public Task<IReadOnlyList<MonthlyProfit>> GetMonthlyProfitAsync(CancellationToken cancellationToken = default)
    {
        return _orderRepository.GetMonthlyProfitAsync(cancellationToken);
    }
}
