using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using B2B.Application.Common.Interface;
using B2B.Application.Inventory.Dtos;
using B2B.Domain.Common;
using B2B.Domain.Inventory;
using B2B.Domain.Skus;
using MediatR;

namespace B2B.Application.Inventory.Commands.Reserve
{
    public sealed class ReserveInventoryCommandHandler
        : IRequestHandler<ReserveInventoryCommand, ReserveResultDto>
    {
        private readonly ISkuRepository _skuRepository;
        private readonly IInventoryReservationRepository _reservationRepository;
        private readonly IIdempotencyStore _idempotencyStore;
        private readonly ITransactionManager _transactionManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _clock;

        public ReserveInventoryCommandHandler(
            ISkuRepository skuRepository,
            IInventoryReservationRepository reservationRepository,
            IIdempotencyStore idempotencyStore,
            ITransactionManager transactionManager,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock)
        {
            _skuRepository = skuRepository;
            _reservationRepository = reservationRepository;
            _idempotencyStore = idempotencyStore;
            _transactionManager = transactionManager;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<ReserveResultDto> Handle(
            ReserveInventoryCommand request,
            CancellationToken ct)
        {
            // Идемпотентность по idempotency_key (повтор reserve → 200 без действий)
            if (await _idempotencyStore.ExistsAsync(request.IdempotencyKey, ct))
                return new ReserveResultDto(request.OrderId, "RESERVED", _clock.UtcNow);

            // Агрегируем по sku_id: один INSERT в inventory_reservations на пару (order_id, sku_id)
            var requested = request.Items
                .GroupBy(i => i.SkuId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            var skuIds = requested.Keys.ToList();

            await _transactionManager.BeginAsync(ct);
            try
            {
                var skus = await _skuRepository.GetByIdsForUpdateAsync(skuIds, ct);
                var skuMap = skus.ToDictionary(s => s.Id);

                // Все проверки до мутаций (all-or-nothing)
                var problems = new List<string>();
                foreach (var (skuId, qty) in requested)
                {
                    if (!skuMap.TryGetValue(skuId, out var sku) || sku.Deleted)
                    {
                        problems.Add($"{skuId}: not found");
                        continue;
                    }
                    if (sku.ActiveQuantity < qty)
                        problems.Add($"{skuId}: insufficient stock (have {sku.ActiveQuantity}, need {qty})");
                }

                if (problems.Count > 0)
                {
                    await _transactionManager.RollbackAsync(ct);
                    throw new DomainException(
                        $"Cannot reserve: {string.Join("; ", problems)}",
                        "INSUFFICIENT_STOCK");
                }

                // Применяем reserve в домене + создаём записи резерва
                var reservations = new List<InventoryReservation>(requested.Count);
                foreach (var (skuId, qty) in requested)
                {
                    skuMap[skuId].Reserve(qty);
                    reservations.Add(new InventoryReservation(
                        id: Guid.NewGuid(),
                        orderId: request.OrderId,
                        skuId: skuId,
                        quantity: qty,
                        createdAt: _clock.UtcNow));
                }
                await _reservationRepository.AddRangeAsync(reservations, ct);

                _idempotencyStore.Register(
                    request.IdempotencyKey,
                    messageType: "inventory.reserve",
                    source: "b2c",
                    payload: JsonSerializer.Serialize(new { request.OrderId, items = request.Items }));

                await _unitOfWork.SaveChangesAsync(ct);
                await _transactionManager.CommitAsync(ct);

                return new ReserveResultDto(request.OrderId, "RESERVED", _clock.UtcNow);
            }
            catch
            {
                await _transactionManager.RollbackAsync(ct);
                throw;
            }
        }
    }
}