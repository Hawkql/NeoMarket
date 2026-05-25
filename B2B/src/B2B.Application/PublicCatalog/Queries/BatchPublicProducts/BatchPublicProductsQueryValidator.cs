using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.PublicCatalog.Queries.BatchPublicProducts
{
    public sealed class BatchPublicProductsQueryValidator
    : AbstractValidator<BatchPublicProductsQuery>
    {
        public BatchPublicProductsQueryValidator()
        {
            RuleFor(x => x.ProductIds)
                .NotNull().WithMessage("product_ids is required")
                .Must(ids => ids.Count <= 100)
                .WithMessage("product_ids must contain at most 100 items");
        }
    }
}
