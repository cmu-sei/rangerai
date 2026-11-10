using System.Text;
using System.Text.Json;

namespace RangerAi.Infrastructure.Filters;

public class RequestLoggingFilter
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingFilter> _logger;

    public RequestLoggingFilter(RequestDelegate next, ILogger<RequestLoggingFilter> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        // Enable buffering so we can read the body multiple times
        context.Request.EnableBuffering();

        string body = "";
        if (context.Request.ContentLength > 0 && context.Request.Body.CanRead)
        {
            using var reader = new StreamReader(
                context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        _logger.LogInformation("HTTP {Method} {Path} \nHeaders: {Headers}\nBody: {Body}",
            context.Request.Method,
            context.Request.Path,
            JsonSerializer.Serialize(context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())),
            body);

        await _next(context);
    }
}
