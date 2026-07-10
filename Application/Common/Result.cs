namespace Application.Common;

public class Result
{
    // Test
    public string Value { get; } = default;
    // Test
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; } = default!;

    public Result(bool isSuccess, Error error)
    {
        if ((isSuccess && error != Error.None) || (!isSuccess && error == Error.None))
        {
            throw new InvalidOperationException();
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    // Test
    public Result(string value, bool isSuccess, Error error) {
        if ((isSuccess && error != Error.None) || (!isSuccess && error == Error.None)) {
            throw new InvalidOperationException();
        }

        Value = value;
        IsSuccess = isSuccess;
        Error = error;
    }
    // Test

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    // Test
    public static Result SuccessWithValue(string value) => new(value, true, Error.None);
    // Test
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    public Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue? Value =>
        IsSuccess ? _value! : throw new InvalidOperationException("Failure results cannot have value");
}