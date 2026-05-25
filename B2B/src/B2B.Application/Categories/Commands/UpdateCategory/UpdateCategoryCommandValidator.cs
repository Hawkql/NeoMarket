using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Categories.Commands.UpdateCategory
{
    public sealed class UpdateCategoryCommandValidator
    : AbstractValidator<UpdateCategoryCommand>
    {
        public UpdateCategoryCommandValidator()
        {
            RuleFor(x => x.CategoryId).NotEqual(Guid.Empty);

            When(x => x.Name is not null, () =>
            {
                RuleFor(x => x.Name!)
                    .NotEmpty().WithMessage("name must not be empty")
                    .MaximumLength(255).WithMessage("name must be 1-255 characters");
            });
        }
    }
}
