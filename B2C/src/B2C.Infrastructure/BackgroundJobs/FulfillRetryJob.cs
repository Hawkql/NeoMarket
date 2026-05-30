using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Orders.Commands.CompleteFulfill;
using B2C.Domain.Orders;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace B2C.Infrastructure.BackgroundJobs
{
    public sealed class FulfillRetryJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FulfillRetryJob> _logger;

        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
        private const int BatchSize = 50;

        public FulfillRetryJob(
            IServiceProvider serviceProvider,
            ILogger<FulfillRetryJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FulfillRetryJob started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in FulfillRetryJob");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (TaskCanceledException) { /* normal shutdown */ }
            }

            _logger.LogInformation("FulfillRetryJob stopped");
        }

        private async Task ProcessBatchAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var orders = await orderRepository.ListPendingFulfillAsync(BatchSize, ct);

            if (orders.Count == 0) return;

            _logger.LogInformation("Retrying fulfill for {Count} DELIVERED orders", orders.Count);

            foreach (var order in orders)
            {
                try
                {
                    await mediator.Send(new CompleteFulfillCommand(order.Id), ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Retry fulfill failed for order {OrderId}", order.Id);
                }
            }
        }
    }
}
