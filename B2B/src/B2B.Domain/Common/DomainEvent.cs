using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Domain.Common
{
    public abstract record DomainEvent(DateTime OccurredOn)
    {
        protected DomainEvent() : this(DateTime.UtcNow) { }
    }
}
