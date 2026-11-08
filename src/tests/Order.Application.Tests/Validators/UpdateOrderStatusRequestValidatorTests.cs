using FluentValidation.TestHelper;
using NUnit.Framework;
using Order.Application.Orders.Requests;
using Order.Application.Orders.Validators;

namespace Order.Application.Tests.Validators;

public class UpdateOrderStatusRequestValidatorTests
{
    private readonly UpdateOrderStatusRequestValidator _validator = new();

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void MissingStatus_HasError(string? status)
    {
        var result = _validator.TestValidate(new UpdateOrderStatusRequest { Status = status });

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Test]
    public void StatusGiven_HasNoErrors()
    {
        var result = _validator.TestValidate(new UpdateOrderStatusRequest { Status = "InProgress" });

        result.ShouldNotHaveAnyValidationErrors();
    }
}
