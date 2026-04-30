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
using B2B.Infrastructure.Messaging.IntegrationEvents;
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
        private const int BATCHSIZE = 50;
        private const int MAXRETRIESBATCH = 5;
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
                try { await Task.Delay(PoolInterval, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task ProcessBatch(CancellationToken ct)
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();

            var messages = await db.OutboxMessage
                .Where(m => m.ProcessedOnUtc == null && m.RetryCount < MAXRETRIESBATCH)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(BATCHSIZE)
                .ToListAsync(ct);

            if (messages.Count == 0) return;

            foreach (var msg in messages)
            {
                try
                {
                    var integrationEvent = MapToIntegrationEvent(msg);
                    if (integrationEvent == null)
                    {
                        msg.ProcessedOnUtc = DateTime.UtcNow;  // не маппится — пропускаем
                        msg.Error = "No mapping defined";
                        continue;
                    }

                    await publisher.PublishAsync(
                        topic: integrationEvent.Topic,
                        key: integrationEvent.Key,
                        payload: integrationEvent.Payload,
                        messageId: msg.Id.ToString(),
                        eventType: integrationEvent.EventType,
                        ct);

                    msg.ProcessedOnUtc = DateTime.UtcNow;
                    msg.Error = null;
                }
                catch (Exception ex)
                {
                    msg.RetryCount++;
                    msg.Error = ex.Message;
                    _logger.LogWarning(ex,
                        "Failed to publish outbox message {Id}, retry {Retry}/{Max}",
                        msg.Id, msg.RetryCount, MAXRETRIESBATCH);
                }
            }

            await db.SaveChangesAsync(ct);
        }

        private static IntegrationEventEnvelope? MapToIntegrationEvent(OutboxMessage msg)
        {
            var type = Type.GetType(msg.Type);
            if (type == null) return null;

            var domainEvent = JsonSerializer.Deserialize(msg.Payload, type) as DomainEvent;
            if (domainEvent == null) return null;

            return domainEvent switch
            {
                ProductCreatedEvent e => new IntegrationEventEnvelope(
                    Topic: "product.created.v1",
                    Key: e.ProductId.ToString(),
                    EventType: "ProductCreated",
                    Payload: new ProductCreatedIntegrationEvent(
                        e.ProductId, e.SellerId, e.OccurredOn)),

                ProductUpdatedEvent e => new IntegrationEventEnvelope(
                    Topic: "product.updated.v1",
                    Key: e.ProductId.ToString(),
                    EventType: "ProductUpdated",
                    Payload: new ProductUpdatedIntegrationEvent(
                        e.ProductId, e.SellerId, e.OccurredOn)),

                InvoiceAcceptedEvent e => new IntegrationEventEnvelope(
                    Topic: "inventory.changed.v1",
                    Key: e.InvoiceId.ToString(),
                    EventType: "InventoryChanged",
                    Payload: new InventoryChangedIntegrationEvent(
                        e.InvoiceId,
                        e.SellerId,
                        e.Lines.Select(l => new InventoryChangedLine(l.SkuId, l.Quantity)).ToList(),
                        e.OccurredOn)),

                _ => null
            };
        }

        private record IntegrationEventEnvelope(
            string Topic,
            string Key,
            string EventType,
            object Payload);
    }
}
