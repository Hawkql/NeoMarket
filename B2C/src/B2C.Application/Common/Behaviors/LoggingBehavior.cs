using System;
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
namespace B2C.Application.Common.Behaviors
{
    /// <summary>
    /// Логирует начало и завершение каждого MediatR-запроса с замером времени.
    /// Полезно для observability и поиска медленных endpoints.
    /// </summary>
    public sealed class LoggingBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken ct)
        {
            var name = typeof(TRequest).Name;
            _logger.LogInformation("Handling {RequestName}", name);
            var sw = Stopwatch.StartNew();
            try
            {
                return await next();
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation("Handled {RequestName} in {Ms}ms", name, sw.ElapsedMilliseconds);
            }
        }
    }
}
