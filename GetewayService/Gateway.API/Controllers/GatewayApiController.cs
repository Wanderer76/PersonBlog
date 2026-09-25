using Gateway.API.Api;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Controllers;

public abstract class GatewayApiController(
    ILogger<BaseApiController> logger) : BaseApiController(logger)
{
    protected Task<IActionResult> ExecuteAsync<T>(
        Func<Task<T>> action,
        Func<T, IActionResult> onSuccess,
        CancellationToken cancellationToken,
        string operationName) =>
        ExecuteCoreAsync(
            async () => onSuccess(await action()),
            cancellationToken,
            operationName);

    protected Task<IActionResult> ExecuteAsync(
        Func<Task> action,
        Func<IActionResult> onSuccess,
        CancellationToken cancellationToken,
        string operationName) =>
        ExecuteCoreAsync(
            async () =>
            {
                await action();
                return onSuccess();
            },
            cancellationToken,
            operationName);

    private async Task<IActionResult> ExecuteCoreAsync(
        Func<Task<IActionResult>> action,
        CancellationToken cancellationToken,
        string operationName)
    {
        try
        {
            return await action();
        }
        catch (DownstreamApiException exception)
        {
            _logger.LogWarning(
                "{DownstreamService} rejected {OperationName} with status {StatusCode}",
                exception.ServiceName,
                operationName,
                (int)exception.StatusCode);

            if (!string.IsNullOrWhiteSpace(exception.ResponseBody))
            {
                return new ContentResult
                {
                    StatusCode = (int)exception.StatusCode,
                    Content = exception.ResponseBody,
                    ContentType = exception.ContentType ?? "application/problem+json"
                };
            }

            return Problem(
                statusCode: (int)exception.StatusCode,
                title: $"{exception.ServiceName} request failed");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("{OperationName} timed out", operationName);
            return Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: $"{operationName} timed out");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "{OperationName} service is unavailable", operationName);
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: $"{operationName} service is unavailable");
        }
        catch (InvalidDataException exception)
        {
            _logger.LogError(exception, "{OperationName} service returned an invalid response", operationName);
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: $"{operationName} service returned an invalid response");
        }
    }

}
