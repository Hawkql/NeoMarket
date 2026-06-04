using B2C.Application.Subscriptions.Dtos;
using FluentValidation;

namespace B2C.Application.Subscriptions.Commands.Subscribe
{
    /// <summary>
    /// NotifyOn по openapi имеет default [BACK_IN_STOCK, PRICE_DROP] — клиент может
    /// прислать пустое тело или вообще без тела. Контроллер заменяет это на default
    /// (оба флага) до создания Command, поэтому до валидатора None не доходит.
    /// Защитная проверка всё равно нужна (на случай прямого вызова или тестов).
    /// </summary>
    public sealed class SubscribeCommandValidator : AbstractValidator<SubscribeCommand>
    {
        public SubscribeCommandValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty();
            RuleFor(x => x.NotifyOn)
                .NotEqual(NotifyOnDto.None)
                .WithMessage("notify_on must contain at least one event type");
        }
    }
}