
using Shared.Utils;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

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

public  class Result<TValue>
{
    [JsonPropertyName("value")]
    private readonly TValue? _value;
    [JsonPropertyName("errors")]
    private readonly IReadOnlyList<Error> _errors;

    private Result(TValue value)
    {
        _value = value;
        _errors = [];
    }

    private Result(IReadOnlyList<Error> error)
    {
        _value = default;
        _errors = error;
    }

    [JsonConstructor]
    private Result(TValue value, IReadOnlyList<Error> errors)
    {
        _value = value;
        _errors = errors;
    }

    public TValue Value
    {
        get
        {
            if (IsSuccess)
                return _value;
            else
                throw new InvalidOperationException("there is no value for failure");
        }
    }
    public IReadOnlyList<Error> Errors
    {
        get => _errors;
    }

    [JsonIgnore]
    public bool IsFailure => _errors.Count > 0;

    [JsonIgnore]
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


public class ResultConverter<TValue> : JsonConverter<Result<TValue>>
{
    private readonly FieldInfo? _valueField;
    private readonly FieldInfo? _errorsField;
    private readonly ConstructorInfo? _constructor;

    public ResultConverter()
    {
        var type = typeof(Result<TValue>);
        _valueField = type.GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance);
        _errorsField = type.GetField("_errors", BindingFlags.NonPublic | BindingFlags.Instance);
        _constructor = type.GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(TValue), typeof(IReadOnlyList<Error>)]
        );
    }

    public override Result<TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        TValue? value = default;
        IReadOnlyList<Error> errors = [];

        if (root.TryGetProperty("value", out var valueProp) && valueProp.ValueKind != JsonValueKind.Null)
        {
            value = valueProp.Deserialize<TValue>(options);
        }

        if (root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Array)
        {
            var errorList = new List<Error>();
            foreach (var item in errorsProp.EnumerateArray())
            {
                var error = item.Deserialize<Error>(options);
                if (error != null) errorList.Add(error);
            }
            errors = errorList;
        }

        if (_constructor != null)
        {
            return (Result<TValue>)_constructor.Invoke([value, errors]);
        }

        var fallback = (Result<TValue>)Activator.CreateInstance(typeToConvert, true)!;
        _valueField?.SetValue(fallback, value);
        _errorsField?.SetValue(fallback, errors);
        return fallback;
    }

    public override void Write(Utf8JsonWriter writer, Result<TValue> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("value");
        var fieldValue = _valueField?.GetValue(value);
        JsonSerializer.Serialize(writer, fieldValue, options);

        writer.WritePropertyName("errors");
        var fieldErrors = _errorsField?.GetValue(value);
        JsonSerializer.Serialize(writer, fieldErrors, options);

        writer.WriteEndObject();
    }
}