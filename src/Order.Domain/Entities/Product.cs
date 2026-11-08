namespace Order.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }

    public Guid ServiceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal UnitCost { get; set; }

    public decimal UnitPrice { get; set; }

    public Service Service { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
