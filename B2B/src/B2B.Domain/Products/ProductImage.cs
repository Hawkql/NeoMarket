using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;

namespace B2B.Domain.Products
{
    public class ProductImage : Entity<Guid>
    {
        public Guid ProductId { get; private set; }
        public string Url { get; private set; } = null!;
        public int Order { get; private set; }
        private ProductImage() { }
        
    }

}
