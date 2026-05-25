using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Images.Commands.UploadImage
{
    public sealed class UploadImageCommandValidator
    : AbstractValidator<UploadImageCommand>
    {
        public UploadImageCommandValidator()
        {
            RuleFor(x => x.FileStream)
                .NotNull().WithMessage("file is required");

            RuleFor(x => x.Ordering)
                .GreaterThanOrEqualTo(0);

            // entity_id nullable допустим (неподшитое изображение).
            // Формат/размер файла проверяет IImageValidator в Handler — там magic bytes,
            // которые FluentValidation проверить не может.
        }
    }
}
