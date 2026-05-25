using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Products.Commands.UpdateProduct
{
    public sealed class UpdateProductCommandValidator
    : AbstractValidator<UpdateProductCommand>
    {
        public UpdateProductCommandValidator()
        {
            // Валидируем только переданные поля (PATCH). When → правило активно
            // только если значение присутствует.
            When(x => x.Title is not null, () =>
            {
                RuleFor(x => x.Title!)
                    .NotEmpty().WithMessage("title must not be empty")
                    .MaximumLength(255).WithMessage("title must be 1-255 characters");
            });

            When(x => x.Description is not null, () =>
            {
                RuleFor(x => x.Description!)
                    .MaximumLength(5000).WithMessage("description must be 1-5000 characters");
            });

            When(x => x.CategoryId is not null, () =>
            {
                RuleFor(x => x.CategoryId!.Value)
                    .NotEqual(Guid.Empty).WithMessage("category_id must be a valid uuid");
            });

            When(x => x.Characteristics is not null, () =>
            {
                RuleForEach(x => x.Characteristics!).ChildRules(ch =>
                {
                    ch.RuleFor(c => c.Name).NotEmpty().WithMessage("characteristic name is required");
                    ch.RuleFor(c => c.Value).NotEmpty().WithMessage("characteristic value is required");
                });
            });
        }
    }
}
