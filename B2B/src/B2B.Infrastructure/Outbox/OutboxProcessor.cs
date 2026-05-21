
using B2B.Infrastructure.Outbox.Dispatchers;
using B2B.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace B2B.Infrastructure.Outbox
{
    public sealed class OutboxProcessor : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxProcessor> _logger;

        private const int PollIntervalSeconds = 2;
        private const int BatchSize = 50;
        private const int MaxRetryCount = 10;

        public OutboxProcessor(
            IServiceProvider serviceProvider,
            ILogger<OutboxProcessor> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxProcessor started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // На самом верхнем уровне ловим всё — иначе BackgroundService умрёт
                    _logger.LogError(ex, "Unhandled error in OutboxProcessor");
                }

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(PollIntervalSeconds),
                        stoppingToken);
                }
                catch (TaskCanceledException) { /* normal shutdown */ }
            }

            _logger.LogInformation("OutboxProcessor stopped");
        }

        private async Task ProcessBatchAsync(CancellationToken ct)
        {
            // Каждый цикл создаём свой scope — DbContext scoped, dispatchers тоже
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var registry = scope.ServiceProvider.GetRequiredService<DispatcherRegistry>();

            // Берём неотправленные с ограничением попыток
            var messages = await dbContext.Set<OutboxMessage>()
                .Where(m => m.ProcessedOnUtc == null && m.RetryCount < MaxRetryCount)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (messages.Count == 0) return;

            _logger.LogDebug("Processing {Count} outbox messages", messages.Count);

            foreach (var message in messages)
            {
                try
                {
                    var dispatcher = registry.GetByDestination(message.Destination);
                    await dispatcher.SendAsync(message, ct);

                    message.ProcessedOnUtc = DateTime.UtcNow;
                    message.Error = null;
                }
                catch (Exception ex)
                {
                    message.RetryCount++;
                    message.Error = ex.Message;
                    _logger.LogWarning(
                        ex,
                        "Failed to dispatch outbox {MessageId} ({EventType}, attempt {Retry})",
                        message.Id, message.EventType, message.RetryCount);
                }
            }

            await dbContext.SaveChangesAsync(ct);
        }
    }
}
