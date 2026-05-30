using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using B2C.Api.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace B2C.Api.Tests.HomePage
{
    [Collection("Sequential")]
    public sealed class BannersTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public BannersTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.CatalogFake.Reset();
        }

        /// <summary>
        /// US-CART-04: GET /home/banners возвращает активные баннеры.
        /// Один баннер засеян в CustomWebApplicationFactory.SeedBaseDataAsync (TestData.BannerId).
        /// Authorization не требуется (AllowAnonymous).
        /// </summary>
        [Fact(DisplayName = "banners_return_active_unauthenticated")]
        public async Task get_banners_works_without_authentication()
        {
            var client = _factory.CreateClient();

            var resp = await client.GetAsync("/api/v1/home/banners");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> BANNERS: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var arr = JsonDocument.Parse(body).RootElement;
            arr.GetArrayLength().Should().BeGreaterOrEqualTo(1,
                "seed-баннер из фабрики должен присутствовать");

            // Проверяем, что seed-баннер есть.
            var found = false;
            foreach (var item in arr.EnumerateArray())
                if (item.GetProperty("id").GetGuid() == TestData.BannerId) found = true;
            found.Should().BeTrue();
        }

        /// <summary>
        /// US-CART-04: POST /home/banners/events записывает событие impression/click.
        /// </summary>
        [Fact(DisplayName = "banner_event_recorded")]
        public async Task post_banner_event_returns_204()
        {
            var client = _factory.CreateClient();

            var resp = await client.PostAsJsonAsync("/api/v1/banner-events", new
            {
                banner_id = TestData.BannerId,
                type = "impression",
            });

            resp.StatusCode.Should().Be(HttpStatusCode.NoContent,
                await resp.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// US-CART-04: событие на несуществующий баннер — НЕ ошибка (silent skip с логом).
        /// Это сознательное решение — write-only fire-and-forget семантика.
        /// </summary>
        [Fact(DisplayName = "banner_event_on_unknown_banner_returns_400")]
        public async Task post_banner_event_for_unknown_banner_returns_400_with_code()
        {
            var client = _factory.CreateClient();

            var resp = await client.PostAsJsonAsync("/api/v1/banner-events", new
            {
                banner_id = Guid.NewGuid(),
                type = "click",
            });
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> UNKNOWN BANNER: {resp.StatusCode} BODY: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            JsonDocument.Parse(body).RootElement.GetProperty("code").GetString()
                .Should().Be("BANNER_NOT_FOUND");
        }

        /// <summary>
        /// US-CART-04: невалидный type → 400.
        /// </summary>
        [Fact(DisplayName = "banner_event_invalid_type_returns_400")]
        public async Task post_banner_event_with_invalid_type_returns_400()
        {
            var client = _factory.CreateClient();

            var resp = await client.PostAsJsonAsync("/api/v1/banner-events", new
            {
                banner_id = TestData.BannerId,
                type = "scroll",  // не impression и не click
            });

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}