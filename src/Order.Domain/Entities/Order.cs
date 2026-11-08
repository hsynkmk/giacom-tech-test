namespace Order.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }

    public Guid ResellerId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid StatusId { get; set; }

    public DateTime CreatedDate { get; set; }

    public OrderStatus Status { get; set; } = null!;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
