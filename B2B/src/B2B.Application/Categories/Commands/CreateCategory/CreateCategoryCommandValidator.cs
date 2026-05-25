using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Categories.Commands.CreateCategory
{
    public sealed class CreateCategoryCommandValidator
    : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("name is required")
                .MaximumLength(255).WithMessage("name must be 1-255 characters");
        }
    }
}
