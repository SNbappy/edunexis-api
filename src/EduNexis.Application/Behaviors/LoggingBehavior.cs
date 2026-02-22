using Microsoft.Extensions.Logging;

namespace EduNexis.Application.Behaviors;

public sealed class LoggingBehavior<TMessage, TResponse>(
    ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        CancellationToken ct,
        MessageHandlerDelegate<TMessage, TResponse> next)
    {
        var name = typeof(TMessage).Name;
        logger.LogInformation("[START] {RequestName}", name);
        var response = await next(message, ct);
        logger.LogInformation("[END] {RequestName}", name);
        return response;
    }
}
