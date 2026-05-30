using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2C.Application.Catalog.Queries.ListCatalogProducts
{
    public sealed class ListCatalogProductsQueryValidator : AbstractValidator<ListCatalogProductsQuery>
    {
        public ListCatalogProductsQueryValidator()
        {
            RuleFor(x => x.Search)
                .MinimumLength(3)
                    .WithMessage("Search query must be at least 3 characters")
                .MaximumLength(255)
                    .WithMessage("Search query must be at most 255 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.Search));

            RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
            RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);
            RuleFor(x => x)
                .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
                .WithMessage("MinPrice must be <= MaxPrice");

            RuleFor(x => x.Limit).InclusiveBetween(1, 100);
            RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        }
    }
}
