using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Infrastructure.Integration.Contracts
{
    public sealed class B2bCategoryNodeResponse
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public List<B2bCategoryNodeResponse> Children { get; set; } = new();
    }

    public sealed class B2bBreadcrumbResponse
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
    }

    public sealed class B2bCategoryFiltersResponse
    {
        public List<B2bFilterDefinitionResponse> Filters { get; set; } = new();
        public int? PriceMin { get; set; }
        public int? PriceMax { get; set; }
    }

    public sealed class B2bFilterDefinitionResponse
    {
        public string Slug { get; set; } = null!;
        public string Name { get; set; } = null!;
        public List<B2bFilterValueResponse> Values { get; set; } = new();
    }

    public sealed class B2bFilterValueResponse
    {
        public string Value { get; set; } = null!;
        public int Count { get; set; }
    }
}
