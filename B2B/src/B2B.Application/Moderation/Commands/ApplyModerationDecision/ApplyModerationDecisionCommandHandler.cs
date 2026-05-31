using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
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

        public async Task Handle(ApplyModerationDecisionCommand request, CancellationToken ct)
        {
            if (await _idempotencyStore.ExistsAsync(request.IdempotencyKey, ct))
                return;

            var product = await _productRepository.GetByIdAsync(request.ProductId, ct);
            if (product is null)
                throw new DomainException("Product not found", "NOT_FOUND");

            if (request.EventType == "MODERATED")
            {
                product.Approve();
            }
            else // BLOCKED
            {
                var reason = new BlockingReason(
                    reasonId: request.BlockingReasonId!.Value,
                    comment: request.ModeratorComment);

                var reports = (request.FieldReports ?? Enumerable.Empty<FieldReportInputDto>())
                    .Select(fr => (
                        Field: FieldReportTargetMapper.Map(fr.FieldName),
                        skuId: fr.SkuId,
                        comment: fr.Comment))
                    .ToList();

                var skus = await _skuRepository.GetByProductIdAsync(product.Id, ct);
                var skuIds = skus.Select(s => s.Id).ToList();

                if (request.HardBlock)
                    product.HardBlock(reason, reports, skuIds, _clock.UtcNow);
                else
                    product.Block(reason, reports, skuIds, _clock.UtcNow);
            }

            _idempotencyStore.Register(
                request.IdempotencyKey,
                messageType: "moderation.decision",
                source: "moderation",
                payload: JsonSerializer.Serialize(new
                {
                    request.ProductId,
                    request.EventType,
                    request.HardBlock,
                    request.OccurredAt
                }));

            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}