using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Infrastructure.Outbox.Dispatchers
{
    public interface IIntegrationEventDispatcher
    {
        /// <summary>Какой Destination обслуживает этот dispatcher.</summary>
        string Destination { get; }

        /// <summary>
        /// Отправка сообщения. Бросает исключение при провале — OutboxProcessor
        /// обработает retry-логику.
        /// </summary>
        Task SendAsync(OutboxMessage message, CancellationToken ct);
    }
}
