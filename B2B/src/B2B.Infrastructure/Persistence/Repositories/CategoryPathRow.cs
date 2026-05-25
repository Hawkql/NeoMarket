using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Infrastructure.Persistence.Repositories
{
    internal sealed class CategoryPathRow
    {
        public Guid TargetId { get; set; }
        public int Level { get; set; }
        public string? Path { get; set; }
    }
}
