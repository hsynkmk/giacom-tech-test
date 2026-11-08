using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Orders;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Persistence.Repositories;

namespace Order.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "OrderDatabase";

    // The version is set explicitly instead of using ServerVersion.AutoDetect,
    // which would open a database connection while the app is starting up.
    private static readonly MySqlServerVersion ServerVersion = new(new Version(5, 7));

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<OrderDbContext>(options => options.UseMySql(connectionString, ServerVersion));

        services.AddScoped<IOrderRepository, OrderRepository>();

        services.AddHealthChecks()
            .AddDbContextCheck<OrderDbContext>("database");

        return services;
    }
}
