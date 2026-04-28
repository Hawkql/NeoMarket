using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using B2B.Domain.Common;
using B2B.Domain.Invoices;
using B2B.Domain.Invoices.Events;
using B2B.Domain.Products;
using B2B.Domain.Products.Events;
using B2B.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace B2B.Infrastructure.Persistence.Outbox
{
    public class OutboxProcessor : BackgroundService
    {
        public readonly IServiceProvider _sp;
        private readonly ILogger<OutboxProcessor> _logger;
        private static readonly TimeSpan PoolInterval = TimeSpan.FromSeconds(2);
        public OutboxProcessor(IServiceProvider sp, ILogger<OutboxProcessor> logger)
        {
            _sp = sp;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatch(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox process failed");
                }
                await Task.Delay(PoolInterval, stoppingToken);
            }
        }

        private async Task ProcessBatch(CancellationToken stoppingToken)
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var producer = scope.ServiceProvider.GetRequiredService<IkafkaProducer>();


            var messages = await db.OutboxMessage
                .Where(m => m.ProcessedOnUtc == null && m.RetryCount < 5)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(50)
                .ToListAsync(stoppingToken);

            foreach (var message in messages)
            {
                try
                {
                    var (topic, key, pauload) = MapToIntegrationEvent(message);
                    await producer.ProduceAsync(topic, key, pauload, stoppingToken);
                    message.ProcessedOnUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    message.RetryCount++;
                    message.Error = ex.Message;
                    _logger.LogWarning(ex, "Failed to publish outbox message {Id}", message.Id);
                }
            }
            await db.SaveChangesAsync(stoppingToken);
        }

        private static (string topic, string key, string pauload) MapToIntegrationEvent(OutboxMessage msg)
        {
            var type = Type.GetType(msg.Type)!;
            var domainEvent = (DomainEvent)JsonSerializer.Deserialize(msg.Payload, type)!;

            return domainEvent switch
            {
                ProductCreatedEvent e =>(
                    "product.created",
                    e.productId.ToString(),
                    JsonSerializer.Serialize(
                    new{
                        productId = e.productId,
                        sellerId =e.SellerId,
                        occurredOn=e.OccurredOn
                    })),
                 ProductUpdatedEvent e=>(
                    "product.updated",
                    e.ProductId.ToString(),
                    JsonSerializer.Serialize(new
                    {
                        productId = e.ProductId,
                        sellerId = e.SellerId,
                        occurredOn = e.OccurredOn
                    })),
                  InvoiceAcceptedEvent e=>(
                    "inventory.changed",
                    e.InvoiceId.ToString(),
                    JsonSerializer.Serialize(new
                    {
                        invoiceId = e.InvoiceId,
                        sellerId = e.SellerId,
                        lines = e.Lines,
                        occurredOn = e.OccurredOn
                    })),
                    _=>throw new InvalidOperationException($"Unknown event type: {msg.Type}")
            };
        }
    }
}
