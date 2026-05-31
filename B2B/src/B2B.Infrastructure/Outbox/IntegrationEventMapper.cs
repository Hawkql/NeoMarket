using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using B2B.Application.IntegrationEvents.V1;
using B2B.Domain.Categories.Events;
using B2B.Domain.Common;
using B2B.Domain.Images.Events;
using B2B.Domain.Invoices.Events;
using B2B.Domain.Products.Events;
using B2B.Domain.Skus.Events;

namespace B2B.Infrastructure.Outbox
{
    public sealed class IntegrationEventMapper : IIntegrationEventMapper
    {
        public IEnumerable<MappedIntegrationEvent> Map(DomainEvent domainEvent)
        {
            return domainEvent switch
            {
                // ============ Product ============
                ProductSentToModerationEvent e =>
                    [new MappedIntegrationEvent(
                    new ProductSentToModerationIntegrationEventV1
                    {
                        OccurredOnUtc = e.OccurredOnUtc,
                        ProductId = e.Id,
                        SellerId = e.SellerId,
                        Reason = e.Reason switch
                        {
                            ModerationReason.FirstSkuAdded => "first_sku_added",
                            ModerationReason.Edited        => "edited",
                            _ => throw new ArgumentOutOfRangeException()
                        }
                    },
                    Destination: "moderation",
                    EventType: "product.sent_to_moderation.v1")],

                ProductBlockedEvent e =>
                    [new MappedIntegrationEvent(
                    new ProductBlockedIntegrationEventV1
                    {
                        OccurredOnUtc = e.OccurredOnUtc,
                        ProductId = e.Id,
                        SkuIds = e.SkuIds.ToArray()
                    },
                    "b2c",
                    "product.blocked.v1")],


                ProductHardBlockedEvent e =>
                    [new MappedIntegrationEvent(
                    new ProductHardBlockedIntegrationEventV1
                    {
                        OccurredOnUtc = e.OccurredOnUtc,
                        ProductId = e.ProductId,
                        SkuIds = e.SkuIds.ToArray()
                    },
                    "b2c",
                    "product.hard_blocked.v1")],
                ProductRemovedFromModerationEvent e =>
                    [new MappedIntegrationEvent(
                            new ProductRemovedFromModerationIntegrationEventV1(default, default, default) {
                                OccurredOnUtc = e.OccurredOnUtc,
                                ProductId = e.ProductId,
                                SellerId = e.SellerId
                            },
                            Destination: "moderation",
                            EventType: "product.removed_from_moderation.v1")],
                // Удаление товара уведомляет ДВА сервиса разными событиями
                ProductDeletedEvent e =>
                    [
                        new MappedIntegrationEvent(
                        new ProductDeletedIntegrationEventV1
                        {
                            OccurredOnUtc = e.OccurredOnUtc,
                            ProductId = e.Id,
                            SellerId = e.SellerId,
                            SkuIds = e.SkuIds.ToArray()
                        },
                        "moderation",
                        "product.deleted.v1"),
                    new MappedIntegrationEvent(
                        new ProductDeletedIntegrationEventV1
                        {
                            OccurredOnUtc = e.OccurredOnUtc,
                            ProductId = e.Id,
                            SellerId = e.SellerId,
                            SkuIds = e.SkuIds.ToArray()
                        },
                        "b2c",
                        "product.deleted.v1")
                    ],

                // ============ Sku ============
                SkuOutOfStockEvent e =>
                    [new MappedIntegrationEvent(
                    new SkuOutOfStockIntegrationEventV1
                    {
                        OccurredOnUtc = e.OccurredOnUtc,
                        SkuId = e.SkuId,
                        ProductId = e.ProductId
                    },
                    "b2c",
                    "sku.out_of_stock.v1")],

                // ============ Invoice ============
                InvoiceAcceptedEvent e =>
                    [new MappedIntegrationEvent(
                    new InvoiceAcceptedIntegrationEventV1
                    {
                        OccurredOnUtc = e.OccurredOnUtc,
                        InvoiceId = e.InvoiceId,
                        AcceptedLines = e.AcceptedLines.Select(l =>
                            new InvoiceAcceptedLineV1(l.SkuId, l.AcceptedQuantity)).ToArray()
                    },
                    "b2c",
                    "invoice.accepted.v1")],

                // ============ Category ============
                CategoryRenamedEvent e =>
                    [new MappedIntegrationEvent(
                    new CategoryRenamedIntegrationEventV1
                    {
                        OccurredOnUtc = e.OccurredOnUtc,
                        CategoryId = e.CategoryId,
                        NewName = e.NewName
                    },
                    "b2c",
                    "category.renamed.v1")],

                // ============ Внутренние события (не идут наружу) ============
                ProductCreatedEvent => Enumerable.Empty<MappedIntegrationEvent>(),
                ProductUpdatedEvent => Enumerable.Empty<MappedIntegrationEvent>(),
                ProductApprovedEvent => Enumerable.Empty<MappedIntegrationEvent>(),
                SkuCreatedEvent => Enumerable.Empty<MappedIntegrationEvent>(),
                SkuUpdatedEvent => Enumerable.Empty<MappedIntegrationEvent>(),
                SkuDeletedEvent => Enumerable.Empty<MappedIntegrationEvent>(),
                ImageCreatedEvent => Enumerable.Empty<MappedIntegrationEvent>(),
                ImageDeletedEvent => Enumerable.Empty<MappedIntegrationEvent>(),
                CategoryCreatedEvent => Enumerable.Empty<MappedIntegrationEvent>(),
                CategoryMovedEvent => Enumerable.Empty<MappedIntegrationEvent>(),
                CategoryDeletedEvent => Enumerable.Empty<MappedIntegrationEvent>(),
                InvoiceCreatedEvent => Enumerable.Empty<MappedIntegrationEvent>(),

                _ => Enumerable.Empty<MappedIntegrationEvent>()
            };
        }
    }
}
