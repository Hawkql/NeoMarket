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

namespace B2B.Application.Inventory.Commands.Unreserve
{

    public sealed class UnreserveInventoryCommandHandler
        : IRequestHandler<UnreserveInventoryCommand, InventoryOrderResultDto>
    {
        private readonly ISkuRepository _skuRepository;
        private readonly IIdempotencyStore _idempotencyStore;
        private readonly ITransactionManager _transactionManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _clock;

        public UnreserveInventoryCommandHandler(
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
            UnreserveInventoryCommand request,
            CancellationToken ct)
        {
            // Ключ идемпотентности = order_id + тип операции (отличает от fulfill)
            var key = IdempotencyKeyFactory.FromParts("unreserve", request.OrderId.ToString());

            if (await _idempotencyStore.ExistsAsync(key, ct))
            {
                return new InventoryOrderResultDto(
                    request.OrderId, "UNRESERVED", _clock.UtcNow);
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
                    sku.UnReserve(qty);
                }

                _idempotencyStore.Register(
                    key,
                    messageType: "inventory.unreserve",
                    source: "b2c",
                    payload: JsonSerializer.Serialize(new { request.OrderId, items = request.Items }));

                await _unitOfWork.SaveChangesAsync(ct);
                await _transactionManager.CommitAsync(ct);

                return new InventoryOrderResultDto(request.OrderId, "UNRESERVED", _clock.UtcNow);
            }
            catch
            {
                await _transactionManager.RollbackAsync(ct);
                throw;
            }
        }
    }
}
