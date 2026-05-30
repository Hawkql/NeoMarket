using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Domain.Common
{
    public class DomainException : Exception
    {
        public string Code { get; }

        public DomainException(string? message, string code = "DOMAIN_ERROR") : base(message)
        {
            Code = code;
        }
    }
}
