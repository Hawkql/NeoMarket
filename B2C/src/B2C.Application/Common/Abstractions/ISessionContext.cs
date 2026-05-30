using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Common.Abstractions
{
    public interface ISessionContext
    {
        string? SessionId { get; }
        bool HasSession => !string.IsNullOrWhiteSpace(SessionId);
    }
}
