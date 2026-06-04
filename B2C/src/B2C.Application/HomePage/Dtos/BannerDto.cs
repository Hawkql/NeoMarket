using System;

namespace B2C.Application.HomePage.Dtos
{
    /// <summary>
    /// openapi: Banner. required: id, image_url, link.
    /// Внутреннее поле IsActive у нас в БД есть, но в публичный API не выходит.
    /// </summary>
    public sealed record BannerDto(
        Guid Id,
        string? Title,
        string ImageUrl,
        string? Link,
        int? Ordering,
        DateTime? ActiveFrom,
        DateTime? ActiveTo);
}