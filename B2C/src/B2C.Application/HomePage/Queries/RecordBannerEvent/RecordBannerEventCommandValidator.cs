using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2C.Application.HomePage.Queries.RecordBannerEvent
{
    public sealed class RecordBannerEventCommandValidator : AbstractValidator<RecordBannerEventCommand>
    {
        public RecordBannerEventCommandValidator()
        {
            RuleFor(x => x.BannerId).NotEmpty();
            RuleFor(x => x.SessionId).MaximumLength(128).When(x => x.SessionId is not null);
        }
    }
}
