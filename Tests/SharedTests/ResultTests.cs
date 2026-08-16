using System.Text.Json;
using Shared.Utils;

namespace SharedTests;

public sealed class ResultJsonConverterFactoryTests
{
    [Theory]
    [InlineData(typeof(Result))]
    [InlineData(typeof(Result<int>))]
    [InlineData(typeof(Result<int, DomainError>))]
    public void CanConvert_SupportsEveryResultVariant(Type resultType)
    {
        var factory = new ResultJsonConverterFactory();

        Assert.True(factory.CanConvert(resultType));
    }

    [Fact]
    public void CanConvert_RejectsUnrelatedType()
    {
        var factory = new ResultJsonConverterFactory();

        Assert.False(factory.CanConvert(typeof(string)));
    }

    public sealed record DomainError(string Code) : IResultError;
}

public sealed class ResultTests
{
    [Fact]
    public void Success_HasNoErrors()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_WithError_ContainsThatError()
    {
        var error = new Error("name", "Name is required.");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Same(error, Assert.Single(result.Errors));
    }

    [Fact]
    public void Failure_WithStrings_CreatesExpectedError()
    {
        var result = Result.Failure("name", "Name is required.");

        var error = Assert.Single(result.Errors);
        Assert.Equal("name", error.Key);
        Assert.Equal("Name is required.", error.Message);
    }

    [Fact]
    public void Failure_CopiesErrorCollection()
    {
        var source = new List<Error> { new("first") };
        var result = Result.Failure(source);

        source.Add(new Error("second"));

        Assert.Single(result.Errors);
    }

    [Fact]
    public void Failure_RejectsEmptyErrorCollection()
    {
        Assert.Throws<ArgumentException>(() =>
            Result.Failure(Array.Empty<Error>()));
    }

    [Fact]
    public void Failure_RejectsNullErrorInCollection()
    {
        Assert.Throws<ArgumentException>(() =>
            Result.Failure(new Error[] { new("valid"), null! }));
    }

    [Fact]
    public void JsonSerialization_UsesStableEnvelope()
    {
        var result = Result.Failure("name", "Name is required.");

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result));

        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("value").ValueKind);
        Assert.Single(json.RootElement.GetProperty("errors").EnumerateArray());
        Assert.Equal(2, json.RootElement.EnumerateObject().Count());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void JsonRoundTrip_PreservesState(bool successful)
    {
        var source = successful
            ? Result.Success()
            : Result.Failure("name", "Name is required.");

        var json = JsonSerializer.Serialize(source);
        var restored = JsonSerializer.Deserialize<Result>(json);

        Assert.NotNull(restored);
        Assert.Equal(source.IsSuccess, restored.IsSuccess);
        Assert.Equal(source.Errors.Count, restored.Errors.Count);

        if (!successful)
        {
            var error = Assert.Single(restored.Errors);
            Assert.Equal("name", error.Key);
            Assert.Equal("Name is required.", error.Message);
        }
    }
}

public sealed class ResultOfValueTests
{
    [Fact]
    public void Success_ExposesValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_ThrowsWhenValueIsRead()
    {
        var result = Result<int>.Failure(new Error("number", "Invalid number."));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitValueConversion_CreatesSuccess()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ImplicitErrorConversion_CreatesFailure()
    {
        var error = new Error("number", "Invalid number.");

        Result<int> result = error;

        Assert.True(result.IsFailure);
        Assert.Same(error, Assert.Single(result.Errors));
    }

    [Fact]
    public void ExplicitErrorArrayConversion_CreatesFailure()
    {
        var errors = new[] { new Error("first"), new Error("second") };

        var result = (Result<int>)errors;

        Assert.Equal(2, result.Errors.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void JsonRoundTrip_PreservesState(bool successful)
    {
        var source = successful
            ? Result<int>.Success(42)
            : Result<int>.Failure(new Error("number", "Invalid number."));

        var json = JsonSerializer.Serialize(source);
        var restored = JsonSerializer.Deserialize<Result<int>>(json);

        Assert.NotNull(restored);
        Assert.Equal(source.IsSuccess, restored.IsSuccess);
        Assert.Equal(source.Errors.Count, restored.Errors.Count);

        if (successful)
        {
            Assert.Equal(42, restored.Value);
        }
        else
        {
            var error = Assert.Single(restored.Errors);
            Assert.Equal("number", error.Key);
            Assert.Equal("Invalid number.", error.Message);
        }
    }

    [Fact]
    public void JsonSerialization_UsesStableEnvelope()
    {
        var result = Result<int>.Success(42);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result));

        Assert.Equal(42, json.RootElement.GetProperty("value").GetInt32());
        Assert.Empty(json.RootElement.GetProperty("errors").EnumerateArray());
        Assert.Equal(2, json.RootElement.EnumerateObject().Count());
    }

    [Fact]
    public void JsonDeserialization_RejectsFailureWithValue()
    {
        const string json = """
            {
              "value": 42,
              "errors": [{ "key": "number", "message": "Invalid number." }]
            }
            """;

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Result<int>>(json));
    }
}

public sealed class ResultOfValueAndErrorTests
{
    [Fact]
    public void GenericContract_AllowsSingleErrorType()
    {
        var resultType = typeof(Result<,>).MakeGenericType(typeof(int), typeof(Error));

        Assert.Equal(typeof(Result<int, Error>), resultType);
    }

    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(Error[]))]
    public void GenericContract_RejectsTypesThatDoNotRepresentSingleError(Type errorType)
    {
        Assert.Throws<ArgumentException>(() =>
            typeof(Result<,>).MakeGenericType(typeof(int), errorType));
    }

    [Fact]
    public void Success_ExposesValueAndNoError()
    {
        var result = Result<int, DomainError>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_ExposesDomainErrorAndThrowsWhenValueIsRead()
    {
        var domainError = new DomainError("not_found", "Entity was not found.");

        var result = Result<int, DomainError>.Failure(domainError);

        Assert.True(result.IsFailure);
        Assert.Same(domainError, result.Error);
        Assert.Same(domainError, Assert.Single(result.Errors));
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitConversions_CreateExpectedStates()
    {
        Result<int, DomainError> success = 42;
        var domainError = new DomainError("invalid", "Invalid value.");
        Result<int, DomainError> failure = domainError;

        Assert.Equal(42, success.Value);
        Assert.Same(domainError, failure.Error);
    }

    [Fact]
    public void From_ReturnsSuccessWhenOperationCompletes()
    {
        var result = Result<int, DomainError>.From<InvalidOperationException>(
            () => 42,
            exception => new DomainError("operation", exception.Message));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void From_MapsMatchingExceptionToFailure()
    {
        var result = Result<int, DomainError>.From<InvalidOperationException>(
            () => throw new InvalidOperationException("Broken operation."),
            exception => new DomainError("operation", exception.Message));

        Assert.True(result.IsFailure);
        Assert.Equal("operation", result.Error!.Code);
        Assert.Equal("Broken operation.", result.Error.Message);
    }

    [Fact]
    public void From_DoesNotCatchOtherExceptionTypes()
    {
        Assert.Throws<ArgumentException>(() =>
            Result<int, DomainError>.From<InvalidOperationException>(
                () => throw new ArgumentException("Wrong argument."),
                exception => new DomainError("operation", exception.Message)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void JsonRoundTrip_PreservesState(bool successful)
    {
        var source = successful
            ? Result<int, DomainError>.Success(42)
            : Result<int, DomainError>.Failure(
                new DomainError("not_found", "Entity was not found."));

        var json = JsonSerializer.Serialize(source);
        var restored = JsonSerializer.Deserialize<Result<int, DomainError>>(json);

        Assert.NotNull(restored);
        Assert.Equal(source.IsSuccess, restored.IsSuccess);
        Assert.Equal(source.Errors.Count, restored.Errors.Count);

        if (successful)
        {
            Assert.Equal(42, restored.Value);
        }
        else
        {
            Assert.Equal("not_found", restored.Error!.Code);
            Assert.Equal("Entity was not found.", restored.Error.Message);
        }
    }

    [Fact]
    public void Failure_WithMultipleErrors_RoundTripsEveryError()
    {
        var source = Result<int, DomainError>.Failure(
            new DomainError[]
            {
                new("first", "First error."),
                new("second", "Second error.")
            });

        var json = JsonSerializer.Serialize(source);
        var restored = JsonSerializer.Deserialize<Result<int, DomainError>>(json);

        Assert.NotNull(restored);
        Assert.Equal(2, restored.Errors.Count);
        Assert.Equal("first", restored.Error!.Code);
    }

    [Fact]
    public void JsonSerialization_UsesSameEnvelopeAsOtherResults()
    {
        var result = Result<int, DomainError>.Success(42);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result));

        Assert.Equal(42, json.RootElement.GetProperty("value").GetInt32());
        Assert.Empty(json.RootElement.GetProperty("errors").EnumerateArray());
        Assert.Equal(2, json.RootElement.EnumerateObject().Count());
    }

    public sealed record DomainError(string Code, string Message) : IResultError;
}
