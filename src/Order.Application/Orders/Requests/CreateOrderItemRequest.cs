namespace Order.Application.Orders.Requests;

// Only the product and quantity. The ServiceId is taken from the product when the order is saved.
public class CreateOrderItemRequest
{
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }
}
