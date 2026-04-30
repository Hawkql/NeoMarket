using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2B.Domain.Common
{
    public abstract record DomainEvent(DateTime OccurredOn) : INotification
    {
        protected DomainEvent() : this(DateTime.UtcNow) { }
    }
}
