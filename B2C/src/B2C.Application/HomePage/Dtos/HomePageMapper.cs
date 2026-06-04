using B2C.Domain.HomePage;

namespace B2C.Application.HomePage.Dtos
{
    internal static class HomePageMapper
    {
        public static BannerDto ToBannerDto(Banner b) =>
            new(
                Id: b.Id,
                Title: b.Title,
                ImageUrl: b.ImageUrl,
                Link: b.LinkUrl,                  // domain: LinkUrl → openapi: link
                Ordering: b.Priority,              // domain: Priority → openapi: ordering
                ActiveFrom: b.StartsAt,            // domain: StartsAt → openapi: active_from
                ActiveTo: b.EndsAt);               // domain: EndsAt   → openapi: active_to
    }
}