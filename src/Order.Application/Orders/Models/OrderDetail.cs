namespace Order.Application.Orders.Models;

public class OrderDetail
{
    public Guid Id { get; set; }

    public Guid ResellerId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid StatusId { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    public decimal TotalCost { get; set; }

    public decimal TotalPrice { get; set; }

    public IEnumerable<OrderItemDto> Items { get; set; } = [];
}
