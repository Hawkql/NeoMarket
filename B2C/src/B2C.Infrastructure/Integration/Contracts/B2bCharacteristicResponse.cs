using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Infrastructure.Integration.Contracts
{
    public sealed class B2bCharacteristicResponse
    {
        public string Name { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}
