using Shared.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Common immutable state for all result types.
/// </summary>
/// <typeparam name="TError">The error representation used by the result.</typeparam>
public abstract class ResultBase<TError> where TError : class
{
    private readonly IReadOnlyList<TError> _errors;

    protected ResultBase()
    {
        _errors = Array.Empty<TError>();
    }

    protected ResultBase(IEnumerable<TError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var snapshot = errors.ToArray();
        if (snapshot.Length == 0)
            throw new ArgumentException("A failure must contain at least one error.", nameof(errors));

        if (snapshot.Any(error => error is null))
            throw new ArgumentException("A failure cannot contain null errors.", nameof(errors));

        _errors = Array.AsReadOnly(snapshot);
    }

    public IReadOnlyList<TError> Errors => _errors;

    [JsonIgnore]
    public bool IsFailure => _errors.Count > 0;

    [JsonIgnore]
    public bool IsSuccess => !IsFailure;
}

/// <summary>
/// Result of an operation that returns no value and uses the standard <see cref="Error"/> type.
/// </summary>
public sealed class Result : ResultBase<Error>
{
    private Result()
    {
    }

    private Result(IEnumerable<Error> errors)
        : base(errors)
    {
    }

    public static Result Success() => new();

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new([error]);
    }

    public static Result Failure(IReadOnlyList<Error> errors) => new(errors);

    public static Result Failure(string key, string message) => Failure(new Error(key, message));

    public static Result Failure(string message) => Failure(new Error(message));
}

/// <summary>
/// Result of an operation that returns a value and uses the standard <see cref="Error"/> type.
/// Use <see cref="Result"/> for operations without a return value and
/// <see cref="Result{TValue,TError}"/> only when a domain-specific error type is required.
/// </summary>
[JsonConverter(typeof(ResultJsonConverterFactory))]
public sealed class Result<TValue> : ResultBase<Error>
{
    private readonly TValue? _value;

    private Result(TValue value)
    {
        _value = value;
    }

    private Result(IEnumerable<Error> errors)
        : base(errors)
    {
        _value = default;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("There is no value for failure.");

    public static Result<TValue> Success(TValue value) => new(value);

    public static Result<TValue> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new([error]);
    }

    public static Result<TValue> Failure(IReadOnlyList<Error> errors) => new(errors);

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure(error);

    public static explicit operator Result<TValue>(Error[] errors) => Failure(errors);
}

/// <summary>
/// Result of an operation that returns a value and uses a domain-specific error type.
/// Prefer <see cref="Result{TValue}"/> when the standard <see cref="Error"/> model is sufficient.
/// </summary>
public sealed class Result<TValue, TError> : ResultBase<TError> where TError : class
{
    private readonly TValue? _value;

    private Result(TValue value)
    {
        _value = value;
    }

    private Result(TError error)
        : base(ToErrors(error))
    {
        _value = default;
    }

    public TError? Error => IsFailure ? Errors[0] : null;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("There is no value for failure.");

    public static Result<TValue, TError> Success(TValue value) => new(value);

    public static Result<TValue, TError> Failure(TError error) => new(error);

    public static implicit operator Result<TValue, TError>(TValue value) => Success(value);

    public static implicit operator Result<TValue, TError>(TError error) => Failure(error);

    public static Result<TValue, TError> From<TException>(
        Func<TValue> func,
        Func<TException, TError> errorFactory)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(errorFactory);

        try
        {
            return Success(func());
        }
        catch (TException exception)
        {
            return Failure(errorFactory(exception));
        }
    }

    private static IEnumerable<TError> ToErrors(TError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return [error];
    }
}

/// <summary>
/// Makes the safe JSON representation of <see cref="Result{TValue}"/> available in every service
/// without requiring per-application serializer registration.
/// </summary>
public sealed class ResultJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType &&
        typeToConvert.GetGenericTypeDefinition() == typeof(Result<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(ResultConverter<>).MakeGenericType(valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

public sealed class ResultConverter<TValue> : JsonConverter<Result<TValue>>
{
    public override Result<TValue> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("A result must be represented by a JSON object.");

        var hasValue = root.TryGetProperty("value", out var valueProperty);
        var errors = ReadErrors(root, options);

        if (errors.Count > 0)
        {
            if (hasValue && valueProperty.ValueKind != JsonValueKind.Null)
                throw new JsonException("A failed result cannot contain a value.");

            return Result<TValue>.Failure(errors);
        }

        if (!hasValue)
            throw new JsonException("A successful result must contain a value.");

        var value = valueProperty.Deserialize<TValue>(options);
        return Result<TValue>.Success(value!);
    }

    public override void Write(
        Utf8JsonWriter writer,
        Result<TValue> result,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("value");
        if (result.IsSuccess)
            JsonSerializer.Serialize(writer, result.Value, options);
        else
            writer.WriteNullValue();

        writer.WritePropertyName("errors");
        JsonSerializer.Serialize(writer, result.Errors, options);

        writer.WriteEndObject();
    }

    private static IReadOnlyList<Error> ReadErrors(
        JsonElement root,
        JsonSerializerOptions options)
    {
        if (!root.TryGetProperty("errors", out var errorsProperty) ||
            errorsProperty.ValueKind == JsonValueKind.Null)
        {
            return Array.Empty<Error>();
        }

        if (errorsProperty.ValueKind != JsonValueKind.Array)
            throw new JsonException("The errors property must be an array.");

        var errors = errorsProperty.Deserialize<List<Error>>(options);
        if (errors is null || errors.Any(error => error is null))
            throw new JsonException("The errors property cannot contain null errors.");

        return errors;
    }
}
