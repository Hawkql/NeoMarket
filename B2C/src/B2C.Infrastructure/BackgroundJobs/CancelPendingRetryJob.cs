using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Orders.Commands.RetryUnreserve;
using B2C.Domain.Orders;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace B2C.Infrastructure.BackgroundJobs
{
    public sealed class CancelPendingRetryJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CancelPendingRetryJob> _logger;

        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan RetryThreshold = TimeSpan.FromMinutes(1);
        private const int BatchSize = 50;

        public CancelPendingRetryJob(
            IServiceProvider serviceProvider,
            ILogger<CancelPendingRetryJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CancelPendingRetryJob started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in CancelPendingRetryJob");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (TaskCanceledException) { /* normal shutdown */ }
            }

            _logger.LogInformation("CancelPendingRetryJob stopped");
        }

        private async Task ProcessBatchAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var threshold = DateTime.UtcNow - RetryThreshold;
            var orders = await orderRepository.ListPendingCancelOlderThanAsync(threshold, BatchSize, ct);

            if (orders.Count == 0) return;

            _logger.LogInformation("Retrying unreserve for {Count} CANCEL_PENDING orders", orders.Count);

            foreach (var order in orders)
            {
                try
                {
                    // RetryUnreserveCommand — заканчивается на Command → TransactionBehavior
                    // обернёт в транзакцию, изменения статуса зафиксируются.
                    await mediator.Send(new RetryUnreserveCommand(order.Id), ct);
                }
                catch (Exception ex)
                {
                    // Ошибка одного заказа не должна ломать обработку остальных.
                    _logger.LogWarning(ex,
                        "Retry unreserve failed for order {OrderId}", order.Id);
                }
            }
        }
    }
}
