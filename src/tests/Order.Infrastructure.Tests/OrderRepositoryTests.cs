using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Order.Application.Orders.Requests;
using Order.Domain.Entities;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Persistence.Repositories;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Infrastructure.Tests;

public class OrderRepositoryTests
{
    private SqliteConnection _connection = null!;
    private OrderDbContext _dbContext = null!;
    private OrderRepository _orderRepository = null!;

    private readonly Guid _orderStatusCreatedId = Guid.NewGuid();
    private readonly Guid _orderStatusInProgressId = Guid.NewGuid();
    private readonly Guid _orderStatusFailedId = Guid.NewGuid();
    private readonly Guid _orderStatusCompletedId = Guid.NewGuid();
    private readonly Guid _orderServiceEmailId = Guid.NewGuid();
    private readonly Guid _orderProductEmailId = Guid.NewGuid();

    [SetUp]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlite(_connection)
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .Options;

        _dbContext = new OrderDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        _orderRepository = new OrderRepository(_dbContext);

        await AddReferenceDataAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Test]
    public async Task GetOrdersAsync_ReturnsCorrectNumberOfOrders()
    {
        // Arrange
        await AddOrder(Guid.NewGuid(), 1);
        await AddOrder(Guid.NewGuid(), 2);
        await AddOrder(Guid.NewGuid(), 3);

        // Act
        var orders = await _orderRepository.GetOrdersAsync();

        // Assert
        Assert.That(orders, Has.Count.EqualTo(3));
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
        var orders = await _orderRepository.GetOrdersAsync();

        // Assert
        var order1 = orders.Single(x => x.Id == orderId1);
        var order2 = orders.Single(x => x.Id == orderId2);
        var order3 = orders.Single(x => x.Id == orderId3);

        Assert.Multiple(() =>
        {
            Assert.That(order1.TotalCost, Is.EqualTo(0.8m));
            Assert.That(order1.TotalPrice, Is.EqualTo(0.9m));

            Assert.That(order2.TotalCost, Is.EqualTo(1.6m));
            Assert.That(order2.TotalPrice, Is.EqualTo(1.8m));

            Assert.That(order3.TotalCost, Is.EqualTo(2.4m));
            Assert.That(order3.TotalPrice, Is.EqualTo(2.7m));
        });
    }

    [Test]
    public async Task GetOrderByIdAsync_ReturnsCorrectOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        await AddOrder(orderId, 1);

        // Act
        var order = await _orderRepository.GetOrderByIdAsync(orderId);

        // Assert
        Assert.That(order!.Id, Is.EqualTo(orderId));
    }

    [Test]
    public async Task GetOrderByIdAsync_ReturnsNullForUnknownOrder()
    {
        // Act
        var order = await _orderRepository.GetOrderByIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(order, Is.Null);
    }

    [Test]
    public async Task GetOrderByIdAsync_ReturnsCorrectOrderItemCount()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        await AddOrder(orderId, 1);

        // Act
        var order = await _orderRepository.GetOrderByIdAsync(orderId);

        // Assert
        Assert.That(order!.Items.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetOrderByIdAsync_ReturnsOrderWithCorrectTotals()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        await AddOrder(orderId, 2);

        // Act
        var order = await _orderRepository.GetOrderByIdAsync(orderId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(order!.TotalCost, Is.EqualTo(1.6m));
            Assert.That(order.TotalPrice, Is.EqualTo(1.8m));
        });
    }

    [Test]
    public async Task GetOrdersByStatusIdAsync_ReturnsOnlyOrdersWithThatStatus()
    {
        // Arrange
        var failedOrderId = Guid.NewGuid();
        await AddOrder(failedOrderId, 1, _orderStatusFailedId, DateTime.Now);

        await AddOrder(Guid.NewGuid(), 1, _orderStatusCompletedId, DateTime.Now);
        await AddOrder(Guid.NewGuid(), 1);

        // Act
        var orders = await _orderRepository.GetOrdersByStatusIdAsync(_orderStatusFailedId);

        // Assert
        Assert.That(orders, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(orders[0].Id, Is.EqualTo(failedOrderId));
            Assert.That(orders[0].StatusName, Is.EqualTo("Failed"));
        });
    }

    [TestCase("In Progress")]
    [TestCase("InProgress")]
    [TestCase("inprogress")]
    [TestCase("IN PROGRESS")]
    public async Task GetOrderStatusIdAsync_MatchesStatusIgnoringCaseAndSpaces(string statusName)
    {
        // Act
        var statusId = await _orderRepository.GetOrderStatusIdAsync(statusName);

        // Assert
        Assert.That(statusId, Is.EqualTo(_orderStatusInProgressId));
    }

    [TestCase("Cancelled")]
    [TestCase("")]
    [TestCase(" ")]
    public async Task GetOrderStatusIdAsync_ReturnsNullForUnknownStatus(string statusName)
    {
        // Act
        var statusId = await _orderRepository.GetOrderStatusIdAsync(statusName);

        // Assert
        Assert.That(statusId, Is.Null);
    }

    [Test]
    public async Task UpdateOrderStatusAsync_UpdatesStatus()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        await AddOrder(orderId, 1);

        // Act
        var updated = await _orderRepository.UpdateOrderStatusAsync(orderId, _orderStatusInProgressId);

        // Assert
        var order = await _orderRepository.GetOrderByIdAsync(orderId);
        Assert.Multiple(() =>
        {
            Assert.That(updated, Is.True);
            Assert.That(order!.StatusName, Is.EqualTo("In Progress"));
        });
    }

    [Test]
    public async Task UpdateOrderStatusAsync_ReturnsFalseForUnknownOrder()
    {
        // Act
        var updated = await _orderRepository.UpdateOrderStatusAsync(Guid.NewGuid(), _orderStatusCompletedId);

        // Assert
        Assert.That(updated, Is.False);
    }

    [Test]
    public async Task GetExistingProductIdsAsync_ReturnsOnlyProductsThatExist()
    {
        // Arrange
        var unknownProductId = Guid.NewGuid();

        // Act
        var existing = await _orderRepository.GetExistingProductIdsAsync([_orderProductEmailId, unknownProductId]);

        // Assert
        Assert.That(existing, Is.EquivalentTo(new[] { _orderProductEmailId }));
    }

    [Test]
    public async Task CreateOrderAsync_CreatesOrderWithItems()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = [new CreateOrderItemRequest { ProductId = _orderProductEmailId, Quantity = 5 }]
        };

        // Act
        var orderId = await _orderRepository.CreateOrderAsync(request, _orderStatusCreatedId);

        // Assert
        var order = await _orderRepository.GetOrderByIdAsync(orderId);
        Assert.That(order, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(order!.ResellerId, Is.EqualTo(request.ResellerId));
            Assert.That(order.CustomerId, Is.EqualTo(request.CustomerId));
            Assert.That(order.StatusName, Is.EqualTo("Created"));
            Assert.That(order.TotalCost, Is.EqualTo(4.0m));
            Assert.That(order.TotalPrice, Is.EqualTo(4.5m));
        });

        var item = order!.Items.Single();
        Assert.Multiple(() =>
        {
            Assert.That(item.ProductId, Is.EqualTo(_orderProductEmailId));
            Assert.That(item.ServiceId, Is.EqualTo(_orderServiceEmailId), "ServiceId should come from the product");
            Assert.That(item.Quantity, Is.EqualTo(5));
        });
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
        var profits = await _orderRepository.GetMonthlyProfitAsync();

        // Assert
        Assert.That(profits, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(profits[0].Year, Is.EqualTo(2019));
            Assert.That(profits[0].Month, Is.EqualTo(1));
            Assert.That(profits[0].Profit, Is.EqualTo(3.0m));

            Assert.That(profits[1].Year, Is.EqualTo(2019));
            Assert.That(profits[1].Month, Is.EqualTo(3));
            Assert.That(profits[1].Profit, Is.EqualTo(0.5m));
        });
    }

    private Task AddOrder(Guid orderId, int quantity)
    {
        return AddOrder(orderId, quantity, _orderStatusCreatedId, DateTime.Now);
    }

    private async Task AddOrder(Guid orderId, int quantity, Guid statusId, DateTime createdDate)
    {
        _dbContext.Orders.Add(new OrderEntity
        {
            Id = orderId,
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            CreatedDate = createdDate,
            StatusId = statusId,
            Items =
            [
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ServiceId = _orderServiceEmailId,
                    ProductId = _orderProductEmailId,
                    Quantity = quantity
                }
            ]
        });

        await _dbContext.SaveChangesAsync();
    }

    private async Task AddReferenceDataAsync()
    {
        _dbContext.OrderStatuses.AddRange(
            new OrderStatus { Id = _orderStatusCreatedId, Name = "Created" },
            new OrderStatus { Id = _orderStatusInProgressId, Name = "In Progress" },
            new OrderStatus { Id = _orderStatusFailedId, Name = "Failed" },
            new OrderStatus { Id = _orderStatusCompletedId, Name = "Completed" });

        _dbContext.Services.Add(new Service
        {
            Id = _orderServiceEmailId,
            Name = "Email"
        });

        _dbContext.Products.Add(new Product
        {
            Id = _orderProductEmailId,
            Name = "100GB Mailbox",
            UnitCost = 0.8m,
            UnitPrice = 0.9m,
            ServiceId = _orderServiceEmailId
        });

        await _dbContext.SaveChangesAsync();
    }
}
