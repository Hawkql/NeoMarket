using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2C.Application.Catalog.Queries.GetBreadcrumbs
{
    public sealed class GetBreadcrumbsQueryValidator : AbstractValidator<GetBreadcrumbsQuery>
    {
        public GetBreadcrumbsQueryValidator()
        {
            RuleFor(x => x)
                .Must(x => x.CategoryId.HasValue ^ x.ProductId.HasValue)
                .WithMessage("Provide exactly one of CategoryId or ProductId, not both");
        }
    }
}
