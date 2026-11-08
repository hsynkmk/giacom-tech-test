namespace Order.Domain;

// The status names exactly as they are stored in the order_status table.
public static class OrderStatusNames
{
    public const string Created = "Created";
    public const string InProgress = "In Progress";
    public const string Failed = "Failed";
    public const string Completed = "Completed";
}
