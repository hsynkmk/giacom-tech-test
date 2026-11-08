using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NUnit.Framework;
using Order.Data;
using Order.Data.Entities;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Order.Service.Tests;

public class OrderServiceTests
{
    private IOrderService _orderService;
    private IOrderRepository _orderRepository;
    private OrderContext _orderContext;
    private DbConnection _connection;

    private readonly byte[] _orderStatusCreatedId = Guid.NewGuid().ToByteArray();
    private readonly byte[] _orderStatusInProgressId = Guid.NewGuid().ToByteArray();
    private readonly byte[] _orderStatusFailedId = Guid.NewGuid().ToByteArray();
    private readonly byte[] _orderStatusCompletedId = Guid.NewGuid().ToByteArray();
    private readonly byte[] _orderServiceEmailId = Guid.NewGuid().ToByteArray();
    private readonly byte[] _orderProductEmailId = Guid.NewGuid().ToByteArray();


    [SetUp]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<OrderContext>()
            .UseSqlite(CreateInMemoryDatabase())
            .EnableDetailedErrors(true)
            .EnableSensitiveDataLogging(true)
            .Options;

        _connection = RelationalOptionsExtension.Extract(options).Connection;

        _orderContext = new OrderContext(options);
        _orderContext.Database.EnsureDeleted();
        _orderContext.Database.EnsureCreated();

        _orderRepository = new OrderRepository(_orderContext);
        _orderService = new OrderService(_orderRepository);

        await AddReferenceDataAsync(_orderContext);
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
        _orderContext.Dispose();
    }


    private static DbConnection CreateInMemoryDatabase()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        return connection;
    }

    [Test]
    public async Task GetOrdersAsync_ReturnsCorrectNumberOfOrders()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        await AddOrder(orderId1, 1);

        var orderId2 = Guid.NewGuid();
        await AddOrder(orderId2, 2);

        var orderId3 = Guid.NewGuid();
        await AddOrder(orderId3, 3);

        // Act
        var orders = await _orderService.GetOrdersAsync();

        // Assert
        Assert.AreEqual(3, orders.Count());
    }

    [Test]
    public async Task GetOrdersAsync_ReturnsOrdersWithCorrectTotals()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        await AddOrder(orderId1, 1);

        var orderId2 = Guid.NewGuid();
        await AddOrder(orderId2, 2);

        var orderId3 = Guid.NewGuid();
        await AddOrder(orderId3, 3);

        // Act
        var orders = await _orderService.GetOrdersAsync();

        // Assert
        var order1 = orders.SingleOrDefault(x => x.Id == orderId1);
        var order2 = orders.SingleOrDefault(x => x.Id == orderId2);
        var order3 = orders.SingleOrDefault(x => x.Id == orderId3);

        Assert.AreEqual(0.8m, order1.TotalCost);
        Assert.AreEqual(0.9m, order1.TotalPrice);

        Assert.AreEqual(1.6m, order2.TotalCost);
        Assert.AreEqual(1.8m, order2.TotalPrice);

        Assert.AreEqual(2.4m, order3.TotalCost);
        Assert.AreEqual(2.7m, order3.TotalPrice);
    }

    [Test]
    public async Task GetOrderByIdAsync_ReturnsCorrectOrder()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        await AddOrder(orderId1, 1);

        // Act
        var order = await _orderService.GetOrderByIdAsync(orderId1);

        // Assert
        Assert.AreEqual(orderId1, order.Id);
    }

    [Test]
    public async Task GetOrderByIdAsync_ReturnsCorrectOrderItemCount()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        await AddOrder(orderId1, 1);

        // Act
        var order = await _orderService.GetOrderByIdAsync(orderId1);

        // Assert
        Assert.AreEqual(1, order.Items.Count());
    }

    [Test]
    public async Task GetOrderByIdAsync_ReturnsOrderWithCorrectTotals()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        await AddOrder(orderId1, 2);

        // Act
        var order = await _orderService.GetOrderByIdAsync(orderId1);

        // Assert
        Assert.AreEqual(1.6m, order.TotalCost);
        Assert.AreEqual(1.8m, order.TotalPrice);
    }

    [Test]
    public async Task GetOrdersByStatusAsync_ReturnsOnlyOrdersWithThatStatus()
    {
        // Arrange
        var failedOrderId = Guid.NewGuid();
        await AddOrder(failedOrderId, 1, _orderStatusFailedId, DateTime.Now);

        await AddOrder(Guid.NewGuid(), 1, _orderStatusCompletedId, DateTime.Now);
        await AddOrder(Guid.NewGuid(), 1);

        // Act
        var orders = await _orderService.GetOrdersByStatusAsync("Failed");

        // Assert
        Assert.AreEqual(1, orders.Count());
        Assert.AreEqual(failedOrderId, orders.Single().Id);
        Assert.AreEqual("Failed", orders.Single().StatusName);
    }

    [Test]
    public async Task GetOrdersByStatusAsync_MatchesStatusIgnoringCaseAndSpaces()
    {
        // Arrange
        await AddOrder(Guid.NewGuid(), 1, _orderStatusInProgressId, DateTime.Now);

        // Act
        var orders = await _orderService.GetOrdersByStatusAsync("inprogress");

        // Assert
        Assert.AreEqual(1, orders.Count());
    }

    [Test]
    public async Task GetOrdersByStatusAsync_ReturnsNullForUnknownStatus()
    {
        // Arrange
        await AddOrder(Guid.NewGuid(), 1);

        // Act
        var orders = await _orderService.GetOrdersByStatusAsync("Cancelled");

        // Assert
        Assert.IsNull(orders);
    }

    [Test]
    public async Task UpdateOrderStatusAsync_UpdatesStatus()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        await AddOrder(orderId, 1);

        // Act
        var result = await _orderService.UpdateOrderStatusAsync(orderId, "InProgress");

        // Assert
        var order = await _orderService.GetOrderByIdAsync(orderId);
        Assert.AreEqual(Model.UpdateOrderStatusResult.Updated, result);
        Assert.AreEqual("In Progress", order.StatusName);
    }

    [Test]
    public async Task UpdateOrderStatusAsync_ReturnsOrderNotFoundForUnknownOrder()
    {
        // Act
        var result = await _orderService.UpdateOrderStatusAsync(Guid.NewGuid(), "Completed");

        // Assert
        Assert.AreEqual(Model.UpdateOrderStatusResult.OrderNotFound, result);
    }

    [Test]
    public async Task UpdateOrderStatusAsync_ReturnsInvalidStatusAndLeavesOrderUnchanged()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        await AddOrder(orderId, 1);

        // Act
        var result = await _orderService.UpdateOrderStatusAsync(orderId, "Cancelled");

        // Assert
        var order = await _orderService.GetOrderByIdAsync(orderId);
        Assert.AreEqual(Model.UpdateOrderStatusResult.InvalidStatus, result);
        Assert.AreEqual("Created", order.StatusName);
    }

    [Test]
    public async Task CreateOrderAsync_CreatesOrderWithItems()
    {
        // Arrange
        var request = CreateValidOrderRequest(quantity: 5);

        // Act
        var order = await _orderService.CreateOrderAsync(request);

        // Assert
        Assert.AreEqual(request.ResellerId, order.ResellerId);
        Assert.AreEqual(request.CustomerId, order.CustomerId);
        Assert.AreEqual("Created", order.StatusName);
        Assert.AreEqual(1, order.Items.Count());

        var item = order.Items.Single();
        Assert.AreEqual(new Guid(_orderProductEmailId), item.ProductId);
        Assert.AreEqual(new Guid(_orderServiceEmailId), item.ServiceId);
        Assert.AreEqual(5, item.Quantity);
        Assert.AreEqual(4.0m, order.TotalCost);
        Assert.AreEqual(4.5m, order.TotalPrice);
    }

    [Test]
    public async Task ValidateCreateOrderAsync_ReturnsNoErrorsForValidRequest()
    {
        // Arrange
        var request = CreateValidOrderRequest(quantity: 1);

        // Act
        var errors = await _orderService.ValidateCreateOrderAsync(request);

        // Assert
        Assert.IsEmpty(errors);
    }

    [Test]
    public async Task ValidateCreateOrderAsync_ReturnsErrorForUnknownProduct()
    {
        // Arrange
        var request = CreateValidOrderRequest(quantity: 1);
        request.Items[0].ProductId = Guid.NewGuid();

        // Act
        var errors = await _orderService.ValidateCreateOrderAsync(request);

        // Assert
        Assert.AreEqual(1, errors.Count());
    }

    [Test]
    public async Task ValidateCreateOrderAsync_ReturnsErrorForEmptyCustomerId()
    {
        // Arrange
        var request = CreateValidOrderRequest(quantity: 1);
        request.CustomerId = Guid.Empty;

        // Act
        var errors = await _orderService.ValidateCreateOrderAsync(request);

        // Assert
        Assert.AreEqual(1, errors.Count());
    }

    [Test]
    public async Task GetMonthlyProfitAsync_ReturnsProfitForCompletedOrdersGroupedByMonth()
    {
        // Arrange
        // Each unit makes 0.1 profit (0.9 price - 0.8 cost)
        await AddOrder(Guid.NewGuid(), 10, _orderStatusCompletedId, new DateTime(2019, 1, 5));
        await AddOrder(Guid.NewGuid(), 20, _orderStatusCompletedId, new DateTime(2019, 1, 20));
        await AddOrder(Guid.NewGuid(), 5, _orderStatusCompletedId, new DateTime(2019, 3, 1));
        await AddOrder(Guid.NewGuid(), 100, _orderStatusFailedId, new DateTime(2019, 1, 10));

        // Act
        var profits = (await _orderService.GetMonthlyProfitAsync()).ToList();

        // Assert
        Assert.AreEqual(2, profits.Count);

        Assert.AreEqual(2019, profits[0].Year);
        Assert.AreEqual(1, profits[0].Month);
        Assert.AreEqual(3.0m, profits[0].Profit);

        Assert.AreEqual(2019, profits[1].Year);
        Assert.AreEqual(3, profits[1].Month);
        Assert.AreEqual(0.5m, profits[1].Profit);
    }

    private Model.CreateOrderRequest CreateValidOrderRequest(int quantity)
    {
        return new Model.CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new List<Model.CreateOrderItemRequest>
            {
                new Model.CreateOrderItemRequest
                {
                    ProductId = new Guid(_orderProductEmailId),
                    Quantity = quantity
                }
            }
        };
    }

    private Task AddOrder(Guid orderId, int quantity)
    {
        return AddOrder(orderId, quantity, _orderStatusCreatedId, DateTime.Now);
    }

    private async Task AddOrder(Guid orderId, int quantity, byte[] statusId, DateTime createdDate)
    {
        var orderIdBytes = orderId.ToByteArray();
        _orderContext.Order.Add(new Data.Entities.Order
        {
            Id = orderIdBytes,
            ResellerId = Guid.NewGuid().ToByteArray(),
            CustomerId = Guid.NewGuid().ToByteArray(),
            CreatedDate = createdDate,
            StatusId = statusId,
        });

        _orderContext.OrderItem.Add(new OrderItem
        {
            Id = Guid.NewGuid().ToByteArray(),
            OrderId = orderIdBytes,
            ServiceId = _orderServiceEmailId,
            ProductId = _orderProductEmailId,
            Quantity = quantity
        });

        await _orderContext.SaveChangesAsync();
    }

    private async Task AddReferenceDataAsync(OrderContext orderContext)
    {
        orderContext.OrderStatus.Add(new OrderStatus
        {
            Id = _orderStatusCreatedId,
            Name = "Created",
        });

        orderContext.OrderStatus.Add(new OrderStatus
        {
            Id = _orderStatusInProgressId,
            Name = "In Progress",
        });

        orderContext.OrderStatus.Add(new OrderStatus
        {
            Id = _orderStatusFailedId,
            Name = "Failed",
        });

        orderContext.OrderStatus.Add(new OrderStatus
        {
            Id = _orderStatusCompletedId,
            Name = "Completed",
        });

        orderContext.OrderService.Add(new Data.Entities.OrderService
        {
            Id = _orderServiceEmailId,
            Name = "Email"
        });

        orderContext.OrderProduct.Add(new OrderProduct
        {
            Id = _orderProductEmailId,
            Name = "100GB Mailbox",
            UnitCost = 0.8m,
            UnitPrice = 0.9m,
            ServiceId = _orderServiceEmailId
        });

        await orderContext.SaveChangesAsync();
    }
}
