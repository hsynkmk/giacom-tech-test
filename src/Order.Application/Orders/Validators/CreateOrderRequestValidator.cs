using FluentValidation;
using Order.Application.Orders.Requests;

namespace Order.Application.Orders.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    private readonly IOrderRepository _orderRepository;

    public CreateOrderRequestValidator(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;

        RuleFor(x => x.ResellerId).NotEmpty();

        RuleFor(x => x.CustomerId).NotEmpty();

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("An order must contain at least one item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });

        // Checks every product in a single query instead of one query per item.
        RuleFor(x => x.Items)
            .CustomAsync(ValidateProductsExistAsync)
            .When(x => x.Items is { Count: > 0 });
    }

    private async Task ValidateProductsExistAsync(
        List<CreateOrderItemRequest>? items,
        ValidationContext<CreateOrderRequest> context,
        CancellationToken cancellationToken)
    {
        var productIds = items!
            .Select(x => x.ProductId)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (productIds.Count == 0)
        {
            return;
        }

        var existingProductIds = await _orderRepository.GetExistingProductIdsAsync(productIds, cancellationToken);

        foreach (var missingProductId in productIds.Except(existingProductIds))
        {
            context.AddFailure(nameof(CreateOrderRequest.Items), $"Product '{missingProductId}' does not exist.");
        }
    }
}
