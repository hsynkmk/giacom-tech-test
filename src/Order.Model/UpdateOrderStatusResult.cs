namespace Order.Model;

// controller can map each outcome to its own status code (204 / 404 / 400)

public enum UpdateOrderStatusResult
{
    Updated,
    OrderNotFound,
    InvalidStatus
}
