using System;
using System.ComponentModel.DataAnnotations;

namespace Order.Model;

//only the product and quantity. The ServiceId is taken from the product when the order is saved
public class CreateOrderItemRequest
{
    [Required]
    public Guid? ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]     // In a real project something like FluentValidation can be used instead of these validations,
                                                                                // moreover asynchronous validation is coming with .NET 11 :)
    public int Quantity { get; set; }
}
