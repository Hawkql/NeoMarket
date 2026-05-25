using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using B2B.Application.Common;
using B2B.Application.Common.Abstractions;
using B2B.Application.Common.Interface;
using B2B.Application.Inventory.Dtos;
using B2B.Domain.Skus;
using MediatR;

namespace B2B.Application.Inventory.Commands.Fulfill
{
    public sealed class FulfillInventoryCommandHandler
    : IRequestHandler<FulfillInventoryCommand, InventoryOrderResultDto>
    {
        private readonly ISkuRepository _skuRepository;
        private readonly IIdempotencyStore _idempotencyStore;
        private readonly ITransactionManager _transactionManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _clock;

        public FulfillInventoryCommandHandler(
            ISkuRepository skuRepository,
            IIdempotencyStore idempotencyStore,
            ITransactionManager transactionManager,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock)
        {
            _skuRepository = skuRepository;
            _idempotencyStore = idempotencyStore;
            _transactionManager = transactionManager;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<InventoryOrderResultDto> Handle(
            FulfillInventoryCommand request,
            CancellationToken ct)
        {
            // Ключ = order_id + "fulfill" (отличает от unreserve того же заказа)
            var key = IdempotencyKeyFactory.FromParts("fulfill", request.OrderId.ToString());

            if (await _idempotencyStore.ExistsAsync(key, ct))
            {
                return new InventoryOrderResultDto(
                    request.OrderId, "FULFILLED", _clock.UtcNow);
            }

            var requested = request.Items
            .GroupBy(i => i.SkuId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            var skuIds = requested.Keys.ToList();

            await _transactionManager.BeginAsync(ct);
            try
            {
                var skus = await _skuRepository.GetByIdsForUpdateAsync(skuIds, ct);
                var skuMap = skus.ToDictionary(s => s.Id);

                foreach (var (skuId, qty) in requested)
                {
                    if (!skuMap.TryGetValue(skuId, out var sku))
                        continue;
                    sku.Fulfill(qty);
                }

                _idempotencyStore.Register(
                    key,
                    messageType: "inventory.fulfill",
                    source: "b2c",
                    payload: JsonSerializer.Serialize(new { request.OrderId, items = request.Items }));

                await _unitOfWork.SaveChangesAsync(ct);
                await _transactionManager.CommitAsync(ct);

                return new InventoryOrderResultDto(request.OrderId, "FULFILLED", _clock.UtcNow);
            }
            catch
            {
                await _transactionManager.RollbackAsync(ct);
                throw;
            }
        }
    }
}
