using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2C.Application.Catalog.Queries.GetFacets
{
    public sealed class GetFacetsQueryValidator : AbstractValidator<GetFacetsQuery>
    {
        public GetFacetsQueryValidator()
        {
            RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search is not null);
            RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
            RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);
            RuleFor(x => x)
                .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
                .WithMessage("MinPrice must be <= MaxPrice");
        }
    }
}
