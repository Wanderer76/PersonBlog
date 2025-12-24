
using Shared.Utils;

namespace Infrastructure.Extensions;

public sealed record ApiException
{
    /// <summary>
    /// Short, human-readable summary of the problem.
    /// </summary>
    public string Title { get; init; } = "An error occurred.";

    /// <summary>
    /// HTTP status code (e.g. 400, 404, 500).
    /// </summary>
    public int Status { get; init; } = 400;

    /// <summary>
    /// Optional: specific error instance (e.g. UUID of failed request).
    /// </summary>
    public string? Instance { get; init; }

    // --- Extensions (not in RFC 7807, but widely adopted) ---

    /// <summary>
    /// Machine-readable error codes (e.g. "VALIDATION_FAILED").
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Field-level validation errors.
    /// Key: field name (e.g. "Email", "Items[2].Url")
    /// Value: list of error messages.
    /// </summary>
    public Dictionary<string, string[]>? Errors { get; init; }
}

public static class ApiExceptionExtensions
{
    private const string ValidationErrorTitle = "One or more validation errors occurred.";

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
            Instance = instance,
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
        where TError : class
    {
        return result.IsFailure
            ? errorMapper(result.Error!).ToValidationProblem()
            : null;
    }

    public static ApiException? ToValidationProblem<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TError, string> messageMapper,
        string key = "Global")
        where TError : class
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
            Instance = instance,
            Code = code
        };
    }
}