using FluentValidation.TestHelper;
using NSubstitute;
using NUnit.Framework;
using Order.Application.Orders;
using Order.Application.Orders.Requests;
using Order.Application.Orders.Validators;

namespace Order.Application.Tests.Validators;

public class CreateOrderRequestValidatorTests
{
    private readonly Guid _existingProductId = Guid.NewGuid();

    private IOrderRepository _orderRepository = null!;
    private CreateOrderRequestValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _orderRepository = Substitute.For<IOrderRepository>();
        _orderRepository
            .GetExistingProductIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<IEnumerable<Guid>>().Where(x => x == _existingProductId).ToList());

        _validator = new CreateOrderRequestValidator(_orderRepository);
    }

    [Test]
    public async Task ValidRequest_HasNoErrors()
    {
        var result = await _validator.TestValidateAsync(CreateValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task EmptyResellerId_HasError()
    {
        var request = CreateValidRequest();
        request.ResellerId = Guid.Empty;

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.ResellerId);
    }

    [Test]
    public async Task EmptyCustomerId_HasError()
    {
        var request = CreateValidRequest();
        request.CustomerId = Guid.Empty;

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Test]
    public async Task MissingItems_HasError()
    {
        var request = CreateValidRequest();
        request.Items = null;

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("An order must contain at least one item.");
    }

    [Test]
    public async Task EmptyItems_HasErrorAndDoesNotQueryProducts()
    {
        var request = CreateValidRequest();
        request.Items = [];

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Items);
        await _orderRepository.DidNotReceiveWithAnyArgs().GetExistingProductIdsAsync(default!, default);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task QuantityNotPositive_HasError(int quantity)
    {
        var request = CreateValidRequest();
        request.Items![0].Quantity = quantity;

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    [Test]
    public async Task EmptyProductId_HasError()
    {
        var request = CreateValidRequest();
        request.Items![0].ProductId = Guid.Empty;

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor("Items[0].ProductId");
    }

    [Test]
    public async Task UnknownProduct_HasError()
    {
        var unknownProductId = Guid.NewGuid();
        var request = CreateValidRequest();
        request.Items!.Add(new CreateOrderItemRequest { ProductId = unknownProductId, Quantity = 1 });

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage($"Product '{unknownProductId}' does not exist.");
    }

    [Test]
    public async Task SameProductTwice_IsCheckedInOneQuery()
    {
        var request = CreateValidRequest();
        request.Items!.Add(new CreateOrderItemRequest { ProductId = _existingProductId, Quantity = 2 });

        await _validator.TestValidateAsync(request);

        await _orderRepository.Received(1).GetExistingProductIdsAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Count() == 1),
            Arg.Any<CancellationToken>());
    }

    private CreateOrderRequest CreateValidRequest()
    {
        return new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = [new CreateOrderItemRequest { ProductId = _existingProductId, Quantity = 1 }]
        };
    }
}
