namespace Order.Application.Orders.Models;

public class OrderSummary
{
    public Guid Id { get; set; }

    public Guid ResellerId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid StatusId { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    public int ItemCount { get; set; }

    public decimal TotalCost { get; set; }

    public decimal TotalPrice { get; set; }
}
