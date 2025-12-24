using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Shared.Utils;

public sealed class Result<TValue, TError> where TError : class
{
    private readonly TValue? _value;
    public TError? Error { get; }
    public bool IsSuccess { get; }

    private Result(TValue value)
    {
        Value = value;
        IsSuccess = true;
        Error = null;
    }
    private Result(TError error)
    {
        IsSuccess = false;
        Error = error ?? throw new ArgumentException("invalid error", nameof(error));
    }

    public TValue Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException("there is no value for failure");
            }
            return _value!;
        }
        private init => _value = value;
    }

    public bool IsFailure => !IsSuccess;

    public static Result<TValue, TError> Success(TValue value) => new(value);
    public static Result<TValue, TError> Failure(TError error) => new(error);

    public static implicit operator Result<TValue, TError>(TValue value) => Success(value);

    public static implicit operator Result<TValue, TError>(TError error) => Failure(error);
    public static Result<TValue, TError> From<TException>(
        Func<TValue> func,
        Func<TException, TError> errorFactory)
        where TException : Exception
    {
        try { return Success(func()); }
        catch (TException ex) { return Failure(errorFactory(ex)); }
    }
}

public sealed class Result
{
    private readonly IReadOnlyList<Error> _errors;
    private Result(IReadOnlyList<Error> error)
    {
        _errors = error;
    }

    public IReadOnlyList<Error> Errors => _errors;

    public bool IsFailure => _errors.Count > 0;
    public bool IsSuccess => _errors.Count == 0;

    public static Result Success() => new([]);
    public static Result Failure(Error error) => new([error]);
    public static Result Failure(IReadOnlyList<Error> error) => new(error);
    public static Result Failure(string key, string message) => new([new Error(key, message)]);
    public static Result Failure(string message) => new([new Error(message)]);
}

public sealed class Result<TValue>
{
    private readonly TValue? _value;
    private readonly IReadOnlyList<Error> _errors;

    private Result(TValue value)
    {
        _value = value;
        _errors = [];
    }
    private Result(IReadOnlyList<Error> error)
    {
        _value = default(TValue);
        _errors = error;
    }

    public TValue Value
    {
        get
        {
            if (_errors.Count == 0)
                return _value;
            else
                throw new InvalidOperationException("there is no value for failure");
        }
    }
    public IReadOnlyList<Error> Errors
    {
        get => _errors;
    }

    public bool IsFailure => _errors.Count > 0;
    public bool IsSuccess => !IsFailure;

    public static Result<TValue> Success(TValue value) => new(value);
    public static Result<TValue> Failure(Error error) => new([error]);
    public static Result<TValue> Failure(IReadOnlyList<Error> error) => new(error);

    public static implicit operator Result<TValue>(TValue value)
        => Success(value);
    public static implicit operator Result<TValue>(Error error)
        => Failure(error);
    public static explicit operator Result<TValue>(Error[] error)
        => Failure(error);
}
