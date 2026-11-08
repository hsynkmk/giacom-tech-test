using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Order.Domain.Entities;
using Order.Infrastructure.Persistence;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Api.Tests;

// Hosts the real API in memory, with MySQL swapped for a SQLite in-memory database
// seeded with a small, known set of data.
public class OrderApiFactory : WebApplicationFactory<Program>
{
    public static readonly Guid ExistingOrderId = Guid.NewGuid();
    public static readonly Guid ExistingProductId = Guid.NewGuid();

    private static readonly Guid CreatedStatusId = Guid.NewGuid();
    private static readonly Guid CompletedStatusId = Guid.NewGuid();
    private static readonly Guid EmailServiceId = Guid.NewGuid();

    // The in-memory database only lives as long as this connection stays open.
    private readonly SqliteConnection _connection = new("Filename=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // AddInfrastructure requires a connection string. It isn't used, the DbContext is replaced below.
        builder.UseSetting("ConnectionStrings:OrderDatabase", "server=unused");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<OrderDbContext>>();

            _connection.Open();
            services.AddDbContext<OrderDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        await dbContext.Database.EnsureCreatedAsync();

        dbContext.OrderStatuses.AddRange(
            new OrderStatus { Id = CreatedStatusId, Name = "Created" },
            new OrderStatus { Id = Guid.NewGuid(), Name = "In Progress" },
            new OrderStatus { Id = Guid.NewGuid(), Name = "Failed" },
            new OrderStatus { Id = CompletedStatusId, Name = "Completed" });

        dbContext.Services.Add(new Service { Id = EmailServiceId, Name = "Email" });

        dbContext.Products.Add(new Product
        {
            Id = ExistingProductId,
            ServiceId = EmailServiceId,
            Name = "100GB Mailbox",
            UnitCost = 0.8m,
            UnitPrice = 0.9m
        });

        dbContext.Orders.Add(new OrderEntity
        {
            Id = ExistingOrderId,
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            StatusId = CompletedStatusId,
            CreatedDate = new DateTime(2019, 1, 15),
            Items =
            [
                new OrderItem { Id = Guid.NewGuid(), ProductId = ExistingProductId, ServiceId = EmailServiceId, Quantity = 10 }
            ]
        });

        await dbContext.SaveChangesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}
