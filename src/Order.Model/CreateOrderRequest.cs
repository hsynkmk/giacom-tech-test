using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Order.Model;

// With a plain Guid, a missing field silently becomes Guid.Empty
// and [Required] can't catch it!!! With Guid? a missing field is null, so it fails validation
public class CreateOrderRequest
{
    [Required]
    public Guid? ResellerId { get; set; }

    [Required]
    public Guid? CustomerId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "An order must contain at least one item.")]
    public List<CreateOrderItemRequest> Items { get; set; }
}
