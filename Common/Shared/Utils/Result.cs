using Shared.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Common immutable state for all result types.
/// </summary>
/// <typeparam name="TError">The error representation used by the result.</typeparam>
public abstract class ResultBase<TError> where TError : class, IResultError
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
[JsonConverter(typeof(ResultJsonConverterFactory))]
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
/// <typeparamref name="TError"/> must represent one atomic error and implement
/// <see cref="IResultError"/>; arrays and other error collections are not valid error types.
/// Use <see cref="Failure(IReadOnlyList{TError})"/> to create a failure with multiple errors.
/// </summary>
[JsonConverter(typeof(ResultJsonConverterFactory))]
public sealed class Result<TValue, TError> : ResultBase<TError>
    where TError : class, IResultError
{
    private readonly TValue? _value;

    private Result(TValue value)
    {
        _value = value;
    }

    private Result(IEnumerable<TError> errors)
        : base(errors)
    {
        _value = default;
    }

    public TError? Error => IsFailure ? Errors[0] : null;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("There is no value for failure.");

    public static Result<TValue, TError> Success(TValue value) => new(value);

    public static Result<TValue, TError> Failure(TError error) => new(ToErrors(error));

    public static Result<TValue, TError> Failure(IReadOnlyList<TError> errors) => new(errors);

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
/// Makes the unified JSON representation of every result type available in every service without
/// requiring per-application serializer registration.
/// </summary>
public class ResultJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert == typeof(Result))
            return true;

        if (!typeToConvert.IsGenericType)
            return false;

        var genericType = typeToConvert.GetGenericTypeDefinition();
        return genericType == typeof(Result<>) || genericType == typeof(Result<,>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(Result))
            return new ResultConverter();

        var typeArguments = typeToConvert.GetGenericArguments();
        var converterType = typeArguments.Length switch
        {
            1 => typeof(ResultConverter<>).MakeGenericType(typeArguments),
            2 => typeof(ResultConverter<,>).MakeGenericType(typeArguments),
            _ => throw new NotSupportedException($"Unsupported result type: {typeToConvert}.")
        };

        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

public sealed class ResultConverter : JsonConverter<Result>
{
    public override Result Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = ResultJsonSerialization.GetObjectRoot(document);
        var hasValue = ResultJsonSerialization.TryGetProperty(root, "value", options, out var value);
        var errors = ResultJsonSerialization.ReadErrors<Error>(root, options);

        if (hasValue && value.ValueKind != JsonValueKind.Null)
            throw new JsonException("A result without a return value cannot contain a value.");

        return errors.Count > 0
            ? Result.Failure(errors)
            : Result.Success();
    }

    public override void Write(
        Utf8JsonWriter writer,
        Result result,
        JsonSerializerOptions options) =>
        ResultJsonSerialization.Write<object?, Error>(
            writer,
            result.IsSuccess,
            null,
            result.Errors,
            options);
}

public sealed class ResultConverter<TValue> : JsonConverter<Result<TValue>>
{
    public override Result<TValue> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = ResultJsonSerialization.GetObjectRoot(document);
        var hasValue = ResultJsonSerialization.TryGetProperty(
            root,
            "value",
            options,
            out var valueProperty);
        var errors = ResultJsonSerialization.ReadErrors<Error>(root, options);

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
        ResultJsonSerialization.Write(
            writer,
            result.IsSuccess,
            result.IsSuccess ? result.Value : default,
            result.Errors,
            options);
    }
}

public sealed class ResultConverter<TValue, TError> : JsonConverter<Result<TValue, TError>>
    where TError : class, IResultError
{
    public override Result<TValue, TError> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = ResultJsonSerialization.GetObjectRoot(document);
        var hasValue = ResultJsonSerialization.TryGetProperty(
            root,
            "value",
            options,
            out var valueProperty);
        var errors = ResultJsonSerialization.ReadErrors<TError>(root, options);

        if (errors.Count > 0)
        {
            if (hasValue && valueProperty.ValueKind != JsonValueKind.Null)
                throw new JsonException("A failed result cannot contain a value.");

            return Result<TValue, TError>.Failure(errors);
        }

        if (!hasValue)
            throw new JsonException("A successful result must contain a value.");

        var value = valueProperty.Deserialize<TValue>(options);
        return Result<TValue, TError>.Success(value!);
    }

    public override void Write(
        Utf8JsonWriter writer,
        Result<TValue, TError> result,
        JsonSerializerOptions options)
    {
        ResultJsonSerialization.Write(
            writer,
            result.IsSuccess,
            result.IsSuccess ? result.Value : default,
            result.Errors,
            options);
    }
}

internal static class ResultJsonSerialization
{
    internal static JsonElement GetObjectRoot(JsonDocument document)
    {
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("A result must be represented by a JSON object.");

        return root;
    }

    internal static bool TryGetProperty(
        JsonElement root,
        string propertyName,
        JsonSerializerOptions options,
        out JsonElement value)
    {
        if (root.TryGetProperty(propertyName, out value))
            return true;

        if (options.PropertyNameCaseInsensitive)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    internal static IReadOnlyList<TError> ReadErrors<TError>(
        JsonElement root,
        JsonSerializerOptions options)
        where TError : class, IResultError
    {
        if (!TryGetProperty(root, "errors", options, out var errorsProperty) ||
            errorsProperty.ValueKind == JsonValueKind.Null)
        {
            return Array.Empty<TError>();
        }

        if (errorsProperty.ValueKind != JsonValueKind.Array)
            throw new JsonException("The errors property must be an array.");

        var errors = errorsProperty.Deserialize<List<TError>>(options);
        if (errors is null || errors.Any(error => error is null))
            throw new JsonException("The errors property cannot contain null errors.");

        return errors;
    }

    internal static void Write<TValue, TError>(
        Utf8JsonWriter writer,
        bool isSuccess,
        TValue? value,
        IReadOnlyList<TError> errors,
        JsonSerializerOptions options)
        where TError : class, IResultError
    {
        writer.WriteStartObject();

        writer.WritePropertyName("value");
        if (isSuccess)
            JsonSerializer.Serialize(writer, value, options);
        else
            writer.WriteNullValue();

        writer.WritePropertyName("errors");
        JsonSerializer.Serialize(writer, errors, options);

        writer.WriteEndObject();
    }
}
