
using Shared.Utils;

namespace Infrastructure.Extensions;

public sealed record ApiException
{
    public string Title { get; init; } = "An error occurred.";
    public int Status { get; init; } = 400;
    public object? Data { get; init; }

    public string? Code { get; init; }

    public Dictionary<string, string[]>? Errors { get; init; }
}

public static class ApiExceptionExtensions
{
    private const string ValidationErrorTitle = "One or more validation errors occurred.";

    /// <summary>
    /// Нужно использовать для отдачи клиенту, для общения между сервисами не нужен
    /// </summary>
    /// <param name="errors"></param>
    /// <param name="instance"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static ApiException ToValidationProblem(
        this IReadOnlyList<Error> errors,
        string? instance = null)
    {
        if (errors == null) throw new ArgumentNullException(nameof(errors));

        var fieldErrors = errors
            .GroupBy(e => e.Key ?? "Global")
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Message).ToArray()
            );

        return new ApiException
        {
            Title = ValidationErrorTitle,
            Status = 400,
            Errors = fieldErrors.Count > 0 ? fieldErrors : null,
            Code = "VALIDATION_FAILED"
        };
    }

    public static ApiException? ToValidationProblem(this Result result)
        => result.IsFailure ? result.Errors.ToValidationProblem() : null;

    public static ApiException? ToValidationProblem<T>(this Result<T> result)
        => result.IsFailure ? result.Errors.ToValidationProblem() : null;

    public static ApiException? ToValidationProblem<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TError, IReadOnlyList<Error>> errorMapper)
        where TError : class, IResultError
    {
        return result.IsFailure
            ? errorMapper(result.Error!).ToValidationProblem()
            : null;
    }

    public static ApiException? ToValidationProblem<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TError, string> messageMapper,
        string key = "Global")
        where TError : class, IResultError
    {
        return result.IsFailure
            ? new[] { new Error(key, messageMapper(result.Error!)) }
                .ToValidationProblem()
            : null;
    }

    // Глобальная ошибка без привязки к полям
    public static ApiException ToGlobalError(
        string title,
        int status = 400,
        string? code = null,
        string? instance = null)
    {
        return new ApiException
        {
            Title = title,
            Status = status,
            Code = code
        };
    }
    public static ApiException WithData<TData>(
        this TData data)
    {
        return new ApiException { Data = data };
    }

    public static IReadOnlyList<Error> FromApiException(this ApiException? ex)
    {
        if (ex is null)
            return [new Error("Unknown", "An unexpected error occurred.")];

        if (ex.Errors is { Count: > 0 })
        {
            return ex.Errors
                .SelectMany(kvp => kvp.Value.Select(msg => new Error(kvp.Key, msg)))
                .ToArray();
        }

        var key = ex.Code ?? "Global";
        var message = ex.Title;
        return [new Error(key, message)];
    }
}
