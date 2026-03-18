using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.Text;

namespace Infrastructure.Extensions;

public static class SerilogExtensions
{
    public static void AddSerilogLogger(this IHostBuilder builder, IConfiguration configuration)
    {
        builder.UseSerilog((ctx, services, logger) =>
        {
            var baseConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext();

            logger.WriteTo.Async(wt => wt.Logger(baseConfig.CreateLogger(),true));
        });
    }

    public static void UseSerilogRequestLogger(this IApplicationBuilder builder)
    {
        builder.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            
            options.GetLevel = (httpContext, elapsed, ex) =>
                ex != null || httpContext.Response.StatusCode >= 500
                    ? LogEventLevel.Error
                    : LogEventLevel.Information;

            options.EnrichDiagnosticContext = async (diagnosticContext, httpContext) =>
            {
                var statusCode = httpContext.Response.StatusCode;
                var hasException = httpContext.Features.Get<IExceptionHandlerFeature>()?.Error != null;

                if (statusCode >= 500 || hasException)
                {
                    if (httpContext.Request.ContentLength > 0 && httpContext.Request.ContentLength < 1024 * 10) // ограничение 10 КБ
                    {
                        try
                        {
                            httpContext.Request.EnableBuffering(); // разрешить повторное чтение
                            using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
                            var body = await reader.ReadToEndAsync();
                            diagnosticContext.Set("RequestBody", body);
                        }
                        catch (Exception ex)
                        {
                            diagnosticContext.Set("RequestBodyReadError", ex.Message);
                        }
                        finally
                        {
                            httpContext.Request.Body.Position = 0;
                        }
                    }

                    var exceptionHandlerFeature = httpContext.Features.Get<IExceptionHandlerFeature>();
                    if (exceptionHandlerFeature?.Error != null)
                    {
                        diagnosticContext.Set("Exception", exceptionHandlerFeature.Error);
                    }

                    diagnosticContext.Set("QueryString", httpContext.Request.QueryString.ToString());
                    diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
                }
            };
        });
    }
}
