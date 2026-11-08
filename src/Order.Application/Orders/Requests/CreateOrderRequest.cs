namespace Order.Application.Orders.Requests;

// Validated by CreateOrderRequestValidator. A missing id binds as Guid.Empty, which the validator rejects.
public class CreateOrderRequest
{
    public Guid ResellerId { get; set; }

    public Guid CustomerId { get; set; }

    public List<CreateOrderItemRequest>? Items { get; set; }
}
