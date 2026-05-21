using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Common.Abstractions
{
    public interface ICurrentUserService
    {
        Guid SellerId { get; }
        string Role { get; }
        bool IsAuthenticated { get; }
    }
}
