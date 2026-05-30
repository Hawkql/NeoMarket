using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2C.Application.HomePage.Queries.GetCollection
{
    public sealed class GetCollectionQueryValidator : AbstractValidator<GetCollectionQuery>
    {
        public GetCollectionQueryValidator()
        {
            RuleFor(x => x.Slug).NotEmpty().MaximumLength(100);
        }
    }
}
