using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;
using B2C.Domain.HomePage;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2C.Application.HomePage.Queries.RecordBannerEvent
{
    /// <summary>
    /// Шаги:
    ///   1. Проверить, что баннер существует (lightweight ExistsAsync).
    ///      Если баннера нет — НЕ бросаем NOT_FOUND, а молча пропускаем.
    ///      Причина: события могут прийти с устаревшего фронта, где у юзера в кэше старые id.
    ///      Делать клиенту 404 за это — плохой UX.
    ///   2. BannerEvent.Record — фабрика домена.
    ///   3. Сохранить в репозиторий.
    /// 
    /// Это write-only операция: ничего не возвращаем, контроллер отдаст 204.
    /// </summary>
    public sealed class RecordBannerEventCommandHandler : IRequestHandler<RecordBannerEventCommand>
    {
        private readonly IBannerRepository _bannerRepository;
        private readonly IBannerEventRepository _bannerEventRepository;

        public RecordBannerEventCommandHandler(
            IBannerRepository bannerRepository,
            IBannerEventRepository bannerEventRepository)
        {
            _bannerRepository = bannerRepository;
            _bannerEventRepository = bannerEventRepository;
        }

        public async Task Handle(RecordBannerEventCommand request, CancellationToken ct)
        {
            if (!await _bannerRepository.ExistsAsync(request.BannerId, ct))
                throw new DomainException(
                    $"Banner {request.BannerId} not found", "BANNER_NOT_FOUND");

            var @event = BannerEvent.Record(
                request.BannerId, request.BuyerId, request.SessionId, request.Type);

            await _bannerEventRepository.AddAsync(@event, ct);
        }
    }
}
