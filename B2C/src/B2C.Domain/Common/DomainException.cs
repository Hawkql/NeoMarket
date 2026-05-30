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
        public object? Details { get; }

        public DomainException(string? message, string code = "DOMAIN_ERROR", object? details = null)
            : base(message)
        {
            Code = code;
            Details = details;
        }
    }
}
