namespace Order.Domain.Entities;

// A service is the category a product belongs to, e.g. "Email".
public class Service
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = new List<Product>();

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
