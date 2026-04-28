using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Products.Commands.CreateProduct
{
    public class CreateProductCommandValidator :AbstractValidator<CreateProductCommand>
    {
        public CreateProductCommandValidator()
        {
            RuleFor(x=>x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(255);
            RuleFor(x=>x.Description)
                .MaximumLength(3000);
            RuleFor(x => x.CategoryId).NotEmpty();
            RuleFor(x=>x.SelleryId).NotEmpty();
        }
    }
}
