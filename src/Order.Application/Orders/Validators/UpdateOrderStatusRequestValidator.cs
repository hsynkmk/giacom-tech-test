using FluentValidation;
using Order.Application.Orders.Requests;

namespace Order.Application.Orders.Validators;

public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        // Whether the status actually exists is checked by OrderService, as it needs the database.
        RuleFor(x => x.Status).NotEmpty();
    }
}
