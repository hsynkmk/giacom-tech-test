using Order.Application.Common;

namespace Order.Application.Orders;

public static class OrderErrors
{
    public static Error OrderNotFound(Guid orderId) =>
        Error.NotFound($"Order '{orderId}' was not found.");

    public static Error UnknownStatus(string status) =>
        Error.Validation("Status", $"Order status '{status}' does not exist.");
}
