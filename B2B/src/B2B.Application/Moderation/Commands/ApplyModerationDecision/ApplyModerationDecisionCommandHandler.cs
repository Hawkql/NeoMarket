using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using B2B.Domain.Common;
using B2B.Domain.Products;
using B2B.Domain.Skus;
using MediatR;

namespace B2B.Application.Moderation.Commands.ApplyModerationDecision
{
    public sealed class ApplyModerationDecisionCommandHandler
    : IRequestHandler<ApplyModerationDecisionCommand>
    {
        private readonly IProductRepository _productRepository;
        private readonly ISkuRepository _skuRepository;
        private readonly IIdempotencyStore _idempotencyStore;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _clock;

        public ApplyModerationDecisionCommandHandler(
            IProductRepository productRepository,
            ISkuRepository skuRepository,
            IIdempotencyStore idempotencyStore,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock)
        {
            _productRepository = productRepository;
            _skuRepository = skuRepository;
            _idempotencyStore = idempotencyStore;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task Handle(
            ApplyModerationDecisionCommand request,
            CancellationToken ct)
        {
            // 1. Идемпотентность входящего события: повтор того же ключа → 200 без действий
            if (await _idempotencyStore.ExistsAsync(request.IdempotencyKey, ct))
                return;

            // 2. Находим товар. Нет → 404
            var product = await _productRepository.GetByIdAsync(request.ProductId, ct);
            if (product is null)
                throw new DomainException("Product not found", "NOT_FOUND");

            // 3. Ветвление по вердикту
            if (request.Status == "MODERATED")
            {
                // OnModeration → Moderated, чистит blocking_reason. Событие ProductApproved.
                product.Approve();
            }
            else // BLOCKED
            {
                // Причина блокировки из запроса
                var reason = new BlockingReason
                {
                    ReasonId = request.BlockingReason!.Id,
                    Title = request.BlockingReason.Title,
                    Comment = request.BlockingReason.Comment
                };

                // field_reports: строка field_name → enum, кортежи для доменного метода
                var reports = (request.FieldReports ?? new List<FieldReportInputDto>())
                    .Select(fr => (
                        Field: FieldReportTargetMapper.Map(fr.FieldName),
                        skuId: fr.SkuId,
                        comment: fr.Comment))
                    .ToList();

                // skuIds для каскадного события PRODUCT_BLOCKED в B2C — все SKU товара
                var skus = await _skuRepository.GetByProductIdAsync(product.Id, ct);
                var skuIds = skus.Select(s => s.Id).ToList();

                if (request.HardBlock)
                    // Любой статус (кроме уже HardBlocked) → HardBlocked. Событие ProductHardBlocked.
                    product.HardBlock(reason, reports, skuIds, _clock.UtcNow);
                else
                    // Только OnModeration → Blocked. Событие ProductBlocked.
                    product.Block(reason, reports, skuIds, _clock.UtcNow);
            }

            // 4. Регистрируем idempotency-ключ в той же транзакции (защита от повторной обработки)
            _idempotencyStore.Register(
                request.IdempotencyKey,
                messageType: "moderation.decision",
                source: "moderation",
                payload: JsonSerializer.Serialize(new
                {
                    request.ProductId,
                    request.Status,
                    request.HardBlock
                }));

            // 5. Сохраняем: Product (статус/reason/field_reports) + Inbox + Outbox-событие
            //    PRODUCT_BLOCKED (для Block/HardBlock) — одна транзакция через SaveChanges.
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
