using Microsoft.EntityFrameworkCore;
using Order.Application.Orders;
using Order.Application.Orders.Models;
using Order.Application.Orders.Requests;
using Order.Domain;
using Order.Domain.Entities;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _dbContext;

    public OrderRepository(OrderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        return await ToOrderSummaries(_dbContext.Orders)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrderSummary>> GetOrdersByStatusIdAsync(Guid statusId, CancellationToken cancellationToken = default)
    {
        var orders = _dbContext.Orders.Where(x => x.StatusId == statusId);

        return await ToOrderSummaries(orders)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderDetail?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Where(x => x.Id == orderId)
            .Select(x => new OrderDetail
            {
                Id = x.Id,
                ResellerId = x.ResellerId,
                CustomerId = x.CustomerId,
                StatusId = x.StatusId,
                StatusName = x.Status.Name,
                CreatedDate = x.CreatedDate,
                TotalCost = Math.Round(x.Items.Sum(i => i.Quantity * i.Product.UnitCost) ?? 0, 2),
                TotalPrice = Math.Round(x.Items.Sum(i => i.Quantity * i.Product.UnitPrice) ?? 0, 2),
                Items = x.Items.Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    OrderId = i.OrderId,
                    ServiceId = i.ServiceId,
                    ServiceName = i.Service.Name,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    UnitCost = i.Product.UnitCost,
                    UnitPrice = i.Product.UnitPrice,
                    TotalCost = Math.Round(i.Product.UnitCost * (i.Quantity ?? 0), 2),
                    TotalPrice = Math.Round(i.Product.UnitPrice * (i.Quantity ?? 0), 2),
                    Quantity = i.Quantity ?? 0
                })
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> GetOrderStatusIdAsync(string statusName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(statusName))
        {
            return null;
        }

        // There are only 4 statuses, so I load them all and compare in memory.
        // That way "InProgress" or "in progress" match "In Progress" (see NormaliseStatusName).
        var statuses = await _dbContext.OrderStatuses
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var status = statuses.FirstOrDefault(x => NormaliseStatusName(x.Name) == NormaliseStatusName(statusName));

        return status?.Id;
    }

    public async Task<bool> UpdateOrderStatusAsync(Guid orderId, Guid statusId, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders.FindAsync([orderId], cancellationToken);
        if (order is null)
        {
            return false;
        }

        order.StatusId = statusId;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyCollection<Guid>> GetExistingProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default)
    {
        var ids = productIds.ToList();

        return await _dbContext.Products
            .Where(x => ids.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    // The request is validated by OrderService before this is called, so every product is known to exist.
    public async Task<Guid> CreateOrderAsync(CreateOrderRequest request, Guid statusId, CancellationToken cancellationToken = default)
    {
        var items = request.Items!;
        var productIds = items.Select(x => x.ProductId).Distinct().ToList();

        // Take the ServiceId from the product instead of asking the caller for it,
        // so an item can never end up with a product from one service and a different ServiceId.
        var serviceIdsByProductId = await _dbContext.Products
            .Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.ServiceId, cancellationToken);

        var order = new OrderEntity
        {
            Id = Guid.NewGuid(),
            ResellerId = request.ResellerId,
            CustomerId = request.CustomerId,
            StatusId = statusId,
            CreatedDate = DateTime.UtcNow
        };

        foreach (var item in items)
        {
            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                ServiceId = serviceIdsByProductId[item.ProductId],
                Quantity = item.Quantity
            });
        }

        // The order and all its items are saved in one SaveChanges, so there's never half an order.
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return order.Id;
    }

    // Profit = quantity * (price - cost), only for completed orders, grouped by the month the order was created.
    public async Task<IReadOnlyList<MonthlyProfit>> GetMonthlyProfitAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrderItems
            .Where(x => x.Order.Status.Name == OrderStatusNames.Completed)
            .GroupBy(x => new { x.Order.CreatedDate.Year, x.Order.CreatedDate.Month })
            .Select(g => new MonthlyProfit
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Profit = Math.Round(g.Sum(x => x.Quantity * (x.Product.UnitPrice - x.Product.UnitCost)) ?? 0, 2)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<OrderSummary> ToOrderSummaries(IQueryable<OrderEntity> orders)
    {
        return orders
            .Select(x => new OrderSummary
            {
                Id = x.Id,
                ResellerId = x.ResellerId,
                CustomerId = x.CustomerId,
                StatusId = x.StatusId,
                StatusName = x.Status.Name,
                ItemCount = x.Items.Count,
                TotalCost = Math.Round(x.Items.Sum(i => i.Quantity * i.Product.UnitCost) ?? 0, 2),
                TotalPrice = Math.Round(x.Items.Sum(i => i.Quantity * i.Product.UnitPrice) ?? 0, 2),
                CreatedDate = x.CreatedDate
            })
            .OrderByDescending(x => x.CreatedDate);
    }

    // The README refers to statuses as "InProgress", but the database stores "In Progress".
    // That's why I wrote this method! Ignoring spaces and case means both styles match the same status
    private static string NormaliseStatusName(string statusName)
    {
        return statusName.Replace(" ", string.Empty).ToLowerInvariant();
    }
}
