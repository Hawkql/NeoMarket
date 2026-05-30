using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2C.Application.Catalog.Queries.GetSimilarProducts
{
    public sealed class GetSimilarProductsQueryValidator : AbstractValidator<GetSimilarProductsQuery>
    {
        public GetSimilarProductsQueryValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty();
            RuleFor(x => x.Limit).InclusiveBetween(1, 50);
        }
    }
}
