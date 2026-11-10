using Microsoft.AspNetCore.SignalR;

namespace RangerAi.Infrastructure.Filters;

public class LoggingHubFilter(ILogger<LoggingHubFilter> logger) : IHubFilter
{
    public async ValueTask<object> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object>> next)
    {
        logger.LogInformation("SignalR: {User} called {Method} with args: {Args}",
            invocationContext.Context.ConnectionId,
            invocationContext.HubMethodName,
            string.Join(", ", invocationContext.HubMethodArguments));

        return await next(invocationContext);
    }
}
