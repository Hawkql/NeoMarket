using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Application.HomePage.Dtos;
using B2C.Domain.HomePage;
using MediatR;

namespace B2C.Application.HomePage.Queries.GetActiveBanners
{
    /// <summary>
    /// Шаги:
    ///   1. Получить видимые на текущий момент баннеры (репозиторий применяет
    ///      Banner.IsVisibleAt(now): IsActive + StartsAt/EndsAt range).
    ///   2. Сортировка по Priority DESC уже сделана в репозитории.
    ///   3. Замапить в BannerDto.
    /// 
    /// Время берётся из IDateTimeProvider — для тестируемости и согласованности
    /// между разными вычислениями в одной секунде.
    /// </summary>
    public sealed class GetActiveBannersQueryHandler
        : IRequestHandler<GetActiveBannersQuery, IReadOnlyList<BannerDto>>
    {
        private readonly IBannerRepository _bannerRepository;
        private readonly IDateTimeProvider _clock;

        public GetActiveBannersQueryHandler(
            IBannerRepository bannerRepository,
            IDateTimeProvider clock)
        {
            _bannerRepository = bannerRepository;
            _clock = clock;
        }

        public async Task<IReadOnlyList<BannerDto>> Handle(
            GetActiveBannersQuery request, CancellationToken ct)
        {
            var banners = await _bannerRepository.ListVisibleAsync(_clock.UtcNow, ct);
            return banners.Select(HomePageMapper.ToBannerDto).ToList();
        }
    }
}
