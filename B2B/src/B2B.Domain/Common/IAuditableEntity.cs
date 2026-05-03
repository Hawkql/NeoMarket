using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Domain.Common
{
    public interface IAuditableEntity
    {
        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }
    }
}
