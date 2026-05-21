using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Infrastructure.Outbox.Dispatchers
{
    public sealed class DispatcherRegistry
    {
        private readonly Dictionary<string, IIntegrationEventDispatcher> _dispatchers;

        public DispatcherRegistry(IEnumerable<IIntegrationEventDispatcher> dispatchers)
        {
            // Защита от duplicate destinations (если кто-то ошибся в DI)
            _dispatchers = dispatchers.ToDictionary(
                d => d.Destination,
                d => d,
                StringComparer.OrdinalIgnoreCase);
        }

        public IIntegrationEventDispatcher GetByDestination(string destination)
        {
            if (!_dispatchers.TryGetValue(destination, out var dispatcher))
                throw new InvalidOperationException(
                    $"No dispatcher registered for destination '{destination}'");

            return dispatcher;
        }
    }
}
