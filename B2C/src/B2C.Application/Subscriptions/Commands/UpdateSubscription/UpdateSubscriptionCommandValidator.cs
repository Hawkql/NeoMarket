using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Subscriptions.Dtos;
using FluentValidation;

namespace B2C.Application.Subscriptions.Commands.UpdateSubscription
{
    public sealed class UpdateSubscriptionCommandValidator : AbstractValidator<UpdateSubscriptionCommand>
    {
        public UpdateSubscriptionCommandValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty();
            RuleFor(x => x.NotifyOn)
                .NotEqual(NotifyOnDto.None)
                .WithMessage("notify_on must contain at least one event type");
        }
    }
}
