using Microsoft.EntityFrameworkCore;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Order.Data;

public class OrderRepository : IOrderRepository
{
    private readonly OrderContext _orderContext;

    public OrderRepository(OrderContext orderContext)
    {
        _orderContext = orderContext;
    }

    // I moved the OrderSummary into ToOrderSummaries so this and GetOrdersByStatusIdAsync return exactly the same shape without copying it
    public async Task<IEnumerable<OrderSummary>> GetOrdersAsync()
    {
        var orders = await ToOrderSummaries(_orderContext.Order)
            .ToListAsync();

        return orders;
    }

    public async Task<IEnumerable<OrderSummary>> GetOrdersByStatusIdAsync(Guid statusId)
    {
        var statusIdBytes = statusId.ToByteArray();

        // same byte[] comparison trick as GetOrderByIdAsync because the ids are stored as binary(16)
        var query = _orderContext.Order
            .Where(x => _orderContext.Database.IsInMemory() ? x.StatusId.SequenceEqual(statusIdBytes) : x.StatusId == statusIdBytes);

        var orders = await ToOrderSummaries(query)
            .ToListAsync();

        return orders;
    }

    public async Task<Guid?> GetOrderStatusIdAsync(string statusName)
    {
        if (string.IsNullOrWhiteSpace(statusName))
        {
            return null;
        }

        // There are only 4 statuses, so I load them all and compare in memory
        // That way I can match "InProgress" or "in progress" to "In Progress" (see NormaliseStatusName)
        var statuses = await _orderContext.OrderStatus.ToListAsync();

        var status = statuses.FirstOrDefault(x => NormaliseStatusName(x.Name) == NormaliseStatusName(statusName));

        return status == null ? null : new Guid(status.Id);
    }

    // I return false instead of throwing when the order doesn't exist, so the service can turn it into a 404 without using excepptions
    public async Task<bool> UpdateOrderStatusAsync(Guid orderId, Guid statusId)
    {
        var orderIdBytes = orderId.ToByteArray();

        var order = await _orderContext.Order
            .Where(x => _orderContext.Database.IsInMemory() ? x.Id.SequenceEqual(orderIdBytes) : x.Id == orderIdBytes)
            .SingleOrDefaultAsync();

        if (order == null)
        {
            return false;
        }

        order.StatusId = statusId.ToByteArray();
        await _orderContext.SaveChangesAsync();

        return true;
    }

    // I used FindAsync here cuz it looks up by primary key and works with the byte[] id
    public async Task<bool> ProductExistsAsync(Guid productId)
    {
        var product = await _orderContext.OrderProduct.FindAsync(productId.ToByteArray());
        return product != null;
    }

    // The request has already been validated in OrderService, so we assume that the products exist
    public async Task<Guid> CreateOrderAsync(CreateOrderRequest request)
    {
        // Every new order starts as "Created"
        var createdStatusId = await GetOrderStatusIdAsync("Created");

        if (createdStatusId == null)
        {
            throw new InvalidOperationException("The 'Created' order status doesn't exist int the database");
        }

        var orderId = Guid.NewGuid();
        var order = new Entities.Order
        {
            Id = orderId.ToByteArray(),
            ResellerId = request.ResellerId.Value.ToByteArray(),
            CustomerId = request.CustomerId.Value.ToByteArray(),
            StatusId = createdStatusId.Value.ToByteArray(),
            CreatedDate = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            // take the ServiceId from the product instead of asking the caller for it,so an item can never end up with a product from one service and a different ServiceId
            var product = await _orderContext.OrderProduct.FindAsync(item.ProductId.Value.ToByteArray());

            order.Items.Add(new Entities.OrderItem
            {
                Id = Guid.NewGuid().ToByteArray(),
                OrderId = order.Id,
                ProductId = product.Id,
                ServiceId = product.ServiceId,
                Quantity = item.Quantity
            });
        }

        _orderContext.Order.Add(order);
        await _orderContext.SaveChangesAsync();

        return orderId;

    }

    public async Task<IEnumerable<MonthlyProfit>> GetMonthlyProfitAsync()
    {
        // Profit = quantity * (price - cost), only for "Completed" orders.
        // first I  tried grouping by CreatedDate.Year/Month in the database, which worked in the SQLite tests
        // but failed on MySQL (the provider can't translate it). So I calculate the profit per item in SQL
        // and do the grouping by month in memory. For this amount of data it should be fine
        var itemProfits = await _orderContext.OrderItem
            .Where(x => x.Order.Status.Name == "Completed")
            .Select(x => new
            {
                x.Order.CreatedDate,
                Profit = x.Quantity * (x.Product.UnitPrice - x.Product.UnitCost)
            })
            .ToListAsync();

        var profits = itemProfits
            .GroupBy(x => new { x.CreatedDate.Year, x.CreatedDate.Month })
            .Select(g => new MonthlyProfit
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Profit = g.Sum(x => x.Profit ?? 0)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();

        return profits;
    }

    public async Task<OrderDetail> GetOrderByIdAsync(Guid orderId)
    {
        var orderIdBytes = orderId.ToByteArray();

        var order = await _orderContext.Order
            .Where(x => _orderContext.Database.IsInMemory() ? x.Id.SequenceEqual(orderIdBytes) : x.Id == orderIdBytes)
            .Select(x => new OrderDetail
            {
                Id = new Guid(x.Id),
                ResellerId = new Guid(x.ResellerId),
                CustomerId = new Guid(x.CustomerId),
                StatusId = new Guid(x.StatusId),
                StatusName = x.Status.Name,
                CreatedDate = x.CreatedDate,
                TotalCost = x.Items.Sum(i => i.Quantity * i.Product.UnitCost).Value,
                TotalPrice = x.Items.Sum(i => i.Quantity * i.Product.UnitPrice).Value,
                Items = x.Items.Select(i => new Model.OrderItem
                {
                    Id = new Guid(i.Id),
                    OrderId = new Guid(i.OrderId),
                    ServiceId = new Guid(i.ServiceId),
                    ServiceName = i.Service.Name,
                    ProductId = new Guid(i.ProductId),
                    ProductName = i.Product.Name,
                    UnitCost = i.Product.UnitCost,
                    UnitPrice = i.Product.UnitPrice,
                    TotalCost = i.Product.UnitCost * i.Quantity.Value,
                    TotalPrice = i.Product.UnitPrice * i.Quantity.Value,
                    Quantity = i.Quantity.Value
                })
            }).SingleOrDefaultAsync();
        
        return order;
    }

    private static IQueryable<OrderSummary> ToOrderSummaries(IQueryable<Entities.Order> orders)
    {
        return orders
            .Include(x => x.Items)
            .Include(x => x.Status)
            .Select(x => new OrderSummary
            {
                Id = new Guid(x.Id),
                ResellerId = new Guid(x.ResellerId),
                CustomerId = new Guid(x.CustomerId),
                StatusId = new Guid(x.StatusId),
                StatusName = x.Status.Name,
                ItemCount = x.Items.Count,
                TotalCost = x.Items.Sum(i => i.Quantity * i.Product.UnitCost).Value,
                TotalPrice = x.Items.Sum(i => i.Quantity * i.Product.UnitPrice).Value,
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
