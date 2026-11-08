namespace Order.Application.Common;

// The outcome of a service call that can fail in an expected way (bad input, missing data).
// Unexpected failures are still exceptions and are handled by the API's global exception handler.
public class ServiceResult
{
    protected ServiceResult(Error? error)
    {
        Error = error;
    }

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public static ServiceResult Success() => new(null);

    public static ServiceResult Failure(Error error) => new(error);
}

public sealed class ServiceResult<T> : ServiceResult
{
    private readonly T? _value;

    private ServiceResult(T value)
        : base(null)
    {
        _value = value;
    }

    private ServiceResult(Error error)
        : base(error)
    {
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result can't be accessed.");

    public static ServiceResult<T> Success(T value) => new(value);

    public static new ServiceResult<T> Failure(Error error) => new(error);
}
