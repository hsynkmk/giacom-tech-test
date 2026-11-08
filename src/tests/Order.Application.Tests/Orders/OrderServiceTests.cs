using NSubstitute;
using NUnit.Framework;
using Order.Application.Common;
using Order.Application.Orders;
using Order.Application.Orders.Models;
using Order.Application.Orders.Requests;
using Order.Application.Orders.Validators;
using Order.Domain;

namespace Order.Application.Tests.Orders;

public class OrderServiceTests
{
    private readonly Guid _createdStatusId = Guid.NewGuid();
    private readonly Guid _inProgressStatusId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    private IOrderRepository _orderRepository = null!;
    private OrderService _orderService = null!;

    [SetUp]
    public void Setup()
    {
        _orderRepository = Substitute.For<IOrderRepository>();

        _orderRepository.GetOrderStatusIdAsync(OrderStatusNames.Created, Arg.Any<CancellationToken>())
            .Returns(_createdStatusId);
        _orderRepository.GetOrderStatusIdAsync("InProgress", Arg.Any<CancellationToken>())
            .Returns(_inProgressStatusId);
        _orderRepository.GetExistingProductIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([_productId]);

        // The real validators are used, so these tests also cover how validation results are turned into errors.
        _orderService = new OrderService(
            _orderRepository,
            new CreateOrderRequestValidator(_orderRepository),
            new UpdateOrderStatusRequestValidator());
    }

    [Test]
    public async Task GetOrderByIdAsync_UnknownOrder_ReturnsNotFound()
    {
        var result = await _orderService.GetOrderByIdAsync(Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error!.Type, Is.EqualTo(ErrorType.NotFound));
        });
    }

    [Test]
    public async Task GetOrdersByStatusAsync_UnknownStatus_ReturnsValidationErrorForStatus()
    {
        var result = await _orderService.GetOrdersByStatusAsync("Faild");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error!.Type, Is.EqualTo(ErrorType.Validation));
            Assert.That(result.Error.ValidationErrors.Keys, Does.Contain("Status"));
        });
        await _orderRepository.DidNotReceiveWithAnyArgs().GetOrdersByStatusIdAsync(default, default);
    }

    [Test]
    public async Task GetOrdersByStatusAsync_KnownStatus_ReturnsOrders()
    {
        var orders = new List<OrderSummary> { new() { Id = Guid.NewGuid() } };
        _orderRepository.GetOrdersByStatusIdAsync(_inProgressStatusId, Arg.Any<CancellationToken>()).Returns(orders);

        var result = await _orderService.GetOrdersByStatusAsync("InProgress");

        Assert.That(result.Value, Is.SameAs(orders));
    }

    [Test]
    public async Task UpdateOrderStatusAsync_MissingStatus_ReturnsValidationError()
    {
        var result = await _orderService.UpdateOrderStatusAsync(Guid.NewGuid(), new UpdateOrderStatusRequest());

        Assert.That(result.Error!.Type, Is.EqualTo(ErrorType.Validation));
        await _orderRepository.DidNotReceiveWithAnyArgs().UpdateOrderStatusAsync(default, default, default);
    }

    [Test]
    public async Task UpdateOrderStatusAsync_UnknownStatus_ReturnsValidationError()
    {
        var result = await _orderService.UpdateOrderStatusAsync(Guid.NewGuid(), new UpdateOrderStatusRequest { Status = "Bogus" });

        Assert.That(result.Error!.Type, Is.EqualTo(ErrorType.Validation));
        await _orderRepository.DidNotReceiveWithAnyArgs().UpdateOrderStatusAsync(default, default, default);
    }

    [Test]
    public async Task UpdateOrderStatusAsync_UnknownOrder_ReturnsNotFound()
    {
        _orderRepository.UpdateOrderStatusAsync(Arg.Any<Guid>(), _inProgressStatusId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _orderService.UpdateOrderStatusAsync(Guid.NewGuid(), new UpdateOrderStatusRequest { Status = "InProgress" });

        Assert.That(result.Error!.Type, Is.EqualTo(ErrorType.NotFound));
    }

    [Test]
    public async Task UpdateOrderStatusAsync_ValidRequest_Succeeds()
    {
        var orderId = Guid.NewGuid();
        _orderRepository.UpdateOrderStatusAsync(orderId, _inProgressStatusId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _orderService.UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = "InProgress" });

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task CreateOrderAsync_InvalidRequest_ReturnsValidationErrorsAndSavesNothing()
    {
        var request = new CreateOrderRequest { Items = [] };

        var result = await _orderService.CreateOrderAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Error!.Type, Is.EqualTo(ErrorType.Validation));
            Assert.That(result.Error.ValidationErrors.Keys, Is.SupersetOf(new[] { "ResellerId", "CustomerId", "Items" }));
        });
        await _orderRepository.DidNotReceiveWithAnyArgs().CreateOrderAsync(default!, default, default);
    }

    [Test]
    public async Task CreateOrderAsync_ValidRequest_CreatesOrderWithCreatedStatus()
    {
        var orderId = Guid.NewGuid();
        var createdOrder = new OrderDetail { Id = orderId };
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = [new CreateOrderItemRequest { ProductId = _productId, Quantity = 3 }]
        };

        _orderRepository.CreateOrderAsync(request, _createdStatusId, Arg.Any<CancellationToken>()).Returns(orderId);
        _orderRepository.GetOrderByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(createdOrder);

        var result = await _orderService.CreateOrderAsync(request);

        Assert.That(result.Value, Is.SameAs(createdOrder));
    }
}
