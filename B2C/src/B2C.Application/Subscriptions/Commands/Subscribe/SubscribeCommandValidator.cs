using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Subscriptions.Dtos;
using FluentValidation;

namespace B2C.Application.Subscriptions.Commands.Subscribe
{
    public sealed class SubscribeCommandValidator : AbstractValidator<SubscribeCommand>
    {
        public SubscribeCommandValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty();

            // notify_on не должен быть None — пустая подписка бессмысленна.
            RuleFor(x => x.NotifyOn)
                .NotEqual(NotifyOnDto.None)
                .WithMessage("notify_on must contain at least one event type");
        }
    }
}
