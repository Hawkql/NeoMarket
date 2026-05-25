using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.PublicCatalog.Queries.ListPublicProducts
{
    public sealed class ListPublicProductsQueryValidator
    : AbstractValidator<ListPublicProductsQuery>
    {
        public ListPublicProductsQueryValidator()
        {
            RuleFor(x => x.Limit)
                .GreaterThan(0).WithMessage("limit must be > 0")
                .LessThanOrEqualTo(100).WithMessage("limit must be <= 100");

            RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);

            // search по спеке minLength 3 (если задан)
            When(x => x.Search is not null, () =>
            {
                RuleFor(x => x.Search!)
                    .MinimumLength(3).WithMessage("search must be at least 3 characters");
            });

            When(x => x.MinPrice is not null, () =>
                RuleFor(x => x.MinPrice!.Value).GreaterThanOrEqualTo(0));
            When(x => x.MaxPrice is not null, () =>
                RuleFor(x => x.MaxPrice!.Value).GreaterThanOrEqualTo(0));
        }
    }
}
