using System.ComponentModel.DataAnnotations;

namespace Order.Model;

public class UpdateOrderStatusRequest
{
    [Required]
    public string Status { get; set; }
}
