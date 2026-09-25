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
        var envelope = ResultJsonSerialization.Read<object?, Error>(
            ref reader,
            options,
            acceptsValue: false);

        return envelope.Errors.Count > 0
            ? Result.Failure(envelope.Errors)
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
        var envelope = ResultJsonSerialization.Read<TValue, Error>(
            ref reader,
            options,
            acceptsValue: true);

        if (envelope.Errors.Count > 0)
        {
            if (envelope.HasValue && !envelope.ValueIsNull)
                throw new JsonException("A failed result cannot contain a value.");

            return Result<TValue>.Failure(envelope.Errors);
        }

        if (!envelope.HasValue)
            throw new JsonException("A successful result must contain a value.");

        return Result<TValue>.Success(envelope.Value!);
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
        var envelope = ResultJsonSerialization.Read<TValue, TError>(
            ref reader,
            options,
            acceptsValue: true);

        if (envelope.Errors.Count > 0)
        {
            if (envelope.HasValue && !envelope.ValueIsNull)
                throw new JsonException("A failed result cannot contain a value.");

            return Result<TValue, TError>.Failure(envelope.Errors);
        }

        if (!envelope.HasValue)
            throw new JsonException("A successful result must contain a value.");

        return Result<TValue, TError>.Success(envelope.Value!);
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
    internal static ResultJsonEnvelope<TValue, TError> Read<TValue, TError>(
        ref Utf8JsonReader reader,
        JsonSerializerOptions options,
        bool acceptsValue)
        where TError : class, IResultError
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("A result must be represented by a JSON object.");

        var hasValue = false;
        var valueIsNull = false;
        TValue? value = default;
        var hasErrors = false;
        IReadOnlyList<TError> errors = Array.Empty<TError>();
        var reachedEnd = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                reachedEnd = true;
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("A result can contain only JSON properties.");

            var property = GetProperty(ref reader, options);

            if (!reader.Read())
                throw new JsonException("A result property must contain a value.");

            if (property == ResultProperty.Value)
            {
                if (hasValue)
                    throw new JsonException("The value property cannot occur more than once.");

                hasValue = true;
                valueIsNull = reader.TokenType == JsonTokenType.Null;

                if (!acceptsValue && !valueIsNull)
                    throw new JsonException("A result without a return value cannot contain a value.");

                if (acceptsValue && !valueIsNull)
                    value = JsonSerializer.Deserialize<TValue>(ref reader, options);

                continue;
            }

            if (property == ResultProperty.Errors)
            {
                if (hasErrors)
                    throw new JsonException("The errors property cannot occur more than once.");

                hasErrors = true;
                errors = ReadErrors<TError>(ref reader, options);
                continue;
            }

            reader.Skip();
        }

        if (!reachedEnd)
            throw new JsonException("The result JSON object is incomplete.");

        return new ResultJsonEnvelope<TValue, TError>(
            hasValue,
            valueIsNull,
            value,
            errors);
    }

    private static IReadOnlyList<TError> ReadErrors<TError>(
        ref Utf8JsonReader reader,
        JsonSerializerOptions options)
        where TError : class, IResultError
    {
        if (reader.TokenType == JsonTokenType.Null)
            return Array.Empty<TError>();

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("The errors property must be an array.");

        var errors = JsonSerializer.Deserialize<List<TError>>(ref reader, options);
        if (errors is null || errors.Any(error => error is null))
            throw new JsonException("The errors property cannot contain null errors.");

        return errors;
    }

    private static ResultProperty GetProperty(
        ref Utf8JsonReader reader,
        JsonSerializerOptions options)
    {
        if (reader.ValueTextEquals("value"u8))
            return ResultProperty.Value;

        if (reader.ValueTextEquals("errors"u8))
            return ResultProperty.Errors;

        if (!options.PropertyNameCaseInsensitive)
            return ResultProperty.Unknown;

        var propertyName = reader.GetString();
        if (string.Equals(propertyName, "value", StringComparison.OrdinalIgnoreCase))
            return ResultProperty.Value;

        return string.Equals(propertyName, "errors", StringComparison.OrdinalIgnoreCase)
            ? ResultProperty.Errors
            : ResultProperty.Unknown;
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

    private enum ResultProperty
    {
        Unknown,
        Value,
        Errors
    }
}

internal readonly record struct ResultJsonEnvelope<TValue, TError>(
    bool HasValue,
    bool ValueIsNull,
    TValue? Value,
    IReadOnlyList<TError> Errors)
    where TError : class, IResultError;
