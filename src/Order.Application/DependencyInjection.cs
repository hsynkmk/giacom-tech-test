using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Orders;

namespace Order.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();

        // The services run the validators themselves instead of relying on MVC auto-validation,
        // because CreateOrderRequestValidator has an async rule that queries the database.
        services.AddValidatorsFromAssemblyContaining<OrderService>();

        return services;
    }
}
