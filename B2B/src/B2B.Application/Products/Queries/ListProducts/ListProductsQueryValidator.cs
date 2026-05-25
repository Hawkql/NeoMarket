using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Products.Queries.ListProducts
{
    public sealed class ListProductsQueryValidator
    : AbstractValidator<ListProductsQuery>
    {
        public ListProductsQueryValidator()
        {
            RuleFor(x => x.Limit)
                .GreaterThan(0).WithMessage("limit must be > 0")
                .LessThanOrEqualTo(100).WithMessage("limit must be <= 100");

            RuleFor(x => x.Offset)
                .GreaterThanOrEqualTo(0).WithMessage("offset must be >= 0");
        }
    }
}
