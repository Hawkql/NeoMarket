using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Products.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2B.Application.Products.EventHandlers
{
    public class ProductCreatedDomainEventHandler
    : INotificationHandler<ProductCreatedEvent>
    {
        private readonly ILogger<ProductCreatedDomainEventHandler> _logger;

        public ProductCreatedDomainEventHandler(
            ILogger<ProductCreatedDomainEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(ProductCreatedEvent notification, CancellationToken ct)
        {
            _logger.LogInformation(
                "Product {ProductId} created by seller {SellerId}",
                notification.ProductId,
                notification.SellerId);

            // Здесь могут быть побочные эффекты:
            // - отправить welcome-уведомление продавцу
            // - записать в audit log
            // - и т.д.

            return Task.CompletedTask;
        }
    }
}