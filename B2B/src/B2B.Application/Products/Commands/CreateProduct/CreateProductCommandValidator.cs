using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Products.Commands.CreateProduct
{
    public sealed class CreateProductCommandValidator
    : AbstractValidator<CreateProductCommand>
    {
        public CreateProductCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("title is required")
                .MaximumLength(255).WithMessage("title must be 1-255 characters");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("description is required")
                .MaximumLength(5000).WithMessage("description must be 1-5000 characters");

            RuleFor(x => x.CategoryId)
                .NotEqual(Guid.Empty).WithMessage("category_id is required");

            // По спеке: минимум одно изображение
            RuleFor(x => x.Images)
                .NotEmpty().WithMessage("At least one image is required");

            RuleForEach(x => x.Images).ChildRules(img =>
            {
                img.RuleFor(i => i.Url).NotEmpty().WithMessage("image url is required");
                img.RuleFor(i => i.Ordering).GreaterThanOrEqualTo(0);
            });

            RuleForEach(x => x.Characteristics).ChildRules(ch =>
            {
                ch.RuleFor(c => c.Name).NotEmpty().WithMessage("characteristic name is required");
                ch.RuleFor(c => c.Value).NotEmpty().WithMessage("characteristic value is required");
            });
        }
    }
}
