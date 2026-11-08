namespace Order.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public Guid ServiceId { get; set; }

    // Nullable because the column is nullable in the existing database schema.
    public int? Quantity { get; set; }

    public Order Order { get; set; } = null!;

    public Product Product { get; set; } = null!;

    public Service Service { get; set; } = null!;
}
