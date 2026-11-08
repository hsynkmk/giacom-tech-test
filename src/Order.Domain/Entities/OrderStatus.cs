namespace Order.Domain.Entities;

public class OrderStatus
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
