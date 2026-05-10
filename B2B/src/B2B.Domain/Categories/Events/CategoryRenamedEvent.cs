using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;

namespace B2B.Domain.Categories.Events
{
    public sealed record CategoryRenamedEvent(Guid CategoryId,string OldName,string NewName):DomainEvent;
}
