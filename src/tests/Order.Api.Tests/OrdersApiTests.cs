using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Order.Application.Orders.Models;

namespace Order.Api.Tests;

// End-to-end tests through HTTP: routing, model binding, validation and status codes.
// Each test gets a fresh app and database, so tests that change data can't affect each other.
public class OrdersApiTests
{
    private OrderApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task Setup()
    {
        _factory = new OrderApiFactory();
        await _factory.SeedAsync();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task GetOrders_ReturnsOk()
    {
        var orders = await _client.GetFromJsonAsync<List<OrderSummary>>("/orders");

        Assert.That(orders, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetOrderById_UnknownOrder_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/orders/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetOrderById_NotAGuid_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/orders/not-a-guid");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase("Completed")]
    [TestCase("completed")]
    public async Task GetOrdersByStatus_KnownStatus_ReturnsMatchingOrders(string status)
    {
        var orders = await _client.GetFromJsonAsync<List<OrderSummary>>($"/orders/status/{status}");

        Assert.That(orders!.Single().Id, Is.EqualTo(OrderApiFactory.ExistingOrderId));
    }

    [Test]
    public async Task GetOrdersByStatus_UnknownStatus_ReturnsValidationProblem()
    {
        var response = await _client.GetAsync("/orders/status/Bogus");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        // ModelState keys are case-insensitive, so the error is reported under the route parameter's name "status".
        Assert.That(problem!.Errors.Keys, Does.Contain("Status").IgnoreCase);
    }

    [Test]
    public async Task UpdateOrderStatus_ValidStatus_ReturnsNoContentAndUpdatesOrder()
    {
        var response = await _client.PutAsJsonAsync($"/orders/{OrderApiFactory.ExistingOrderId}/status", new { status = "InProgress" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var order = await _client.GetFromJsonAsync<OrderDetail>($"/orders/{OrderApiFactory.ExistingOrderId}");
        Assert.That(order!.StatusName, Is.EqualTo("In Progress"));
    }

    [Test]
    public async Task UpdateOrderStatus_MissingStatus_ReturnsBadRequest()
    {
        var response = await _client.PutAsJsonAsync($"/orders/{OrderApiFactory.ExistingOrderId}/status", new { });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task UpdateOrderStatus_UnknownOrder_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync($"/orders/{Guid.NewGuid()}/status", new { status = "Failed" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task CreateOrder_ValidRequest_ReturnsCreatedWithLocation()
    {
        var request = new
        {
            resellerId = Guid.NewGuid(),
            customerId = Guid.NewGuid(),
            items = new[] { new { productId = OrderApiFactory.ExistingProductId, quantity = 3 } }
        };

        var response = await _client.PostAsJsonAsync("/orders", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var order = await response.Content.ReadFromJsonAsync<OrderDetail>();
        Assert.Multiple(() =>
        {
            Assert.That(response.Headers.Location!.AbsolutePath, Is.EqualTo($"/orders/{order!.Id}"));
            Assert.That(order.StatusName, Is.EqualTo("Created"));
            Assert.That(order.Items.Single().Quantity, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task CreateOrder_InvalidRequest_ReturnsAllValidationErrors()
    {
        var request = new
        {
            resellerId = Guid.Empty,
            customerId = Guid.NewGuid(),
            items = new[] { new { productId = Guid.NewGuid(), quantity = 0 } }
        };

        var response = await _client.PostAsJsonAsync("/orders", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problem!.Errors.Keys, Is.SupersetOf(new[] { "ResellerId", "Items[0].Quantity", "Items" }));
    }

    [Test]
    public async Task CreateOrder_MalformedJson_ReturnsBadRequest()
    {
        var content = new StringContent("{\"resellerId\":", System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/orders", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetMonthlyProfit_ReturnsProfitForCompletedOrders()
    {
        var profits = await _client.GetFromJsonAsync<List<MonthlyProfit>>("/orders/profit/monthly");

        var profit = profits!.Single();
        Assert.Multiple(() =>
        {
            Assert.That(profit.Year, Is.EqualTo(2019));
            Assert.That(profit.Month, Is.EqualTo(1));
            Assert.That(profit.Profit, Is.EqualTo(1.0m));
        });
    }

    [Test]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
