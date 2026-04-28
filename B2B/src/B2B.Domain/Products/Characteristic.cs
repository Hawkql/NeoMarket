using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;

namespace B2B.Domain.Products
{
    public class Characteristic : Entity<Guid>
    {
        public string Name { get; private set; } = null!;
        public string Value { get; private set; } = null!;

        private Characteristic(){}
        public static Characteristic Create(string name, string value)
        {
            return new Characteristic { Id = Guid.NewGuid(), Name = name, Value = value };
        }
    }
}
