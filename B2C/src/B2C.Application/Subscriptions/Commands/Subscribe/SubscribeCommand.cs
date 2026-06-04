using System;
using B2C.Application.Subscriptions.Dtos;
using MediatR;

namespace B2C.Application.Subscriptions.Commands.Subscribe
{
    /// <summary>
    /// openapi: POST /api/v1/favorites/{product_id}/subscribe → 204.
    /// Upsert-семантика: если подписка есть — заменяем NotifyOn на новое значение.
    /// </summary>
    public sealed record SubscribeCommand(
        Guid ProductId,
        NotifyOnDto NotifyOn) : IRequest;
}