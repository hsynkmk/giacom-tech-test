namespace Order.Application.Common;

public sealed class Error
{
    private Error(ErrorType type, string message, IDictionary<string, string[]> validationErrors)
    {
        Type = type;
        Message = message;
        ValidationErrors = validationErrors;
    }

    public ErrorType Type { get; }

    public string Message { get; }

    // Field name -> messages. Only filled for validation errors.
    public IDictionary<string, string[]> ValidationErrors { get; }

    public static Error NotFound(string message) =>
        new(ErrorType.NotFound, message, new Dictionary<string, string[]>());

    public static Error Validation(string field, string message) =>
        new(ErrorType.Validation, message, new Dictionary<string, string[]> { [field] = [message] });

    public static Error Validation(IDictionary<string, string[]> errors) =>
        new(ErrorType.Validation, "One or more validation errors occurred.", errors);
}
