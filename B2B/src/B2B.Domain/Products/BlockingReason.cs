using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;

namespace B2B.Domain.Products
{
    public class BlockingReason
    {
        public Guid ReasonId { get; set; }
        public string Title { get; set; } = null!;
        public string Comment { get; set; } = null!;

        public BlockingReason() { }
        public BlockingReason(Guid id,string title,string comment) 
        {
            if(id== Guid.Empty)
                throw new DomainException("BlockingReason id is required", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("BlockingReason title is required", "INVALID_REQUEST");

            ReasonId = id;
            Title = title;
            Comment = comment;
        }
    }
}
