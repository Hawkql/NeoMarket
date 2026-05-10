using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;

namespace B2B.Domain.Images.Events
{
    public sealed record ImageReorderdEvent(Guid ImageId,
        int NewOrdering) : DomainEvent;
}
