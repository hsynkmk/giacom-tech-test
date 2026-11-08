namespace Order.Application.Orders.Models;

public class OrderItemDto
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid ServiceId { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalCost { get; set; }

    public decimal TotalPrice { get; set; }
}
