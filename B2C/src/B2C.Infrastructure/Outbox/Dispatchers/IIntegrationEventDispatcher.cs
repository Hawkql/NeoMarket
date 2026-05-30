using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Infrastructure.Outbox.Dispatchers
{
    /// <summary>
    /// Диспетчер исходящих integration events. Каждая реализация обслуживает
    /// один Destination (символьное имя сервиса-получателя).
    /// </summary>
    public interface IIntegrationEventDispatcher
    {
        /// <summary>Какой Destination обслуживает этот dispatcher (например, "b2b").</summary>
        string Destination { get; }

        /// <summary>
        /// Отправка сообщения. Бросает исключение при провале — OutboxProcessor
        /// обработает retry-логику (увеличит RetryCount, запишет Error).
        /// </summary>
        Task SendAsync(OutboxMessage message, CancellationToken ct);
    }
}
