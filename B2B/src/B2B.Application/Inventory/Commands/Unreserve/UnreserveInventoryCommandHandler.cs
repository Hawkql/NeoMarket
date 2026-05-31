using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using B2B.Application.Common;
using B2B.Application.Common.Abstractions;
using B2B.Application.Common.Interface;
using B2B.Application.Inventory.Dtos;
using B2B.Domain.Common;
using B2B.Domain.Inventory;
using B2B.Domain.Skus;
using MediatR;

namespace B2B.Application.Inventory.Commands.Unreserve
{
    public sealed class UnreserveInventoryCommandHandler
        : IRequestHandler<UnreserveInventoryCommand, InventoryOrderResultDto>
    {
        private readonly ISkuRepository _skuRepository;
        private readonly IInventoryReservationRepository _reservationRepository;
        private readonly IIdempotencyStore _idempotencyStore;
        private readonly ITransactionManager _transactionManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _clock;

        public UnreserveInventoryCommandHandler(
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

        public async Task<InventoryOrderResultDto> Handle(
            UnreserveInventoryCommand request,
            CancellationToken ct)
        {
            // Идемпотентность по order_id+тип (повтор → 200, ничего не делаем)
            var key = IdempotencyKeyFactory.FromParts("unreserve", request.OrderId.ToString());
            if (await _idempotencyStore.ExistsAsync(key, ct))
                return new InventoryOrderResultDto(request.OrderId, "UNRESERVED", _clock.UtcNow);

            // Тело — то, что присылает B2C. Агрегируем по sku_id (мог прислать дубли).
            var requestedByItems = request.Items
                .GroupBy(i => i.SkuId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            await _transactionManager.BeginAsync(ct);
            try
            {
                // Источник правды — записи в inventory_reservations по order_id
                var storedReservations = await _reservationRepository.GetByOrderAsync(request.OrderId, ct);

                // Если резервов нет вообще — повторный unreserve / неизвестный order_id.
                // Возвращаем 200 идемпотентно (поведение flow: компенсирующая операция
                // не должна падать при повторе).
                if (storedReservations.Count == 0)
                {
                    _idempotencyStore.Register(
                        key, messageType: "inventory.unreserve", source: "b2c",
                        payload: JsonSerializer.Serialize(new { request.OrderId, items = request.Items }));
                    await _unitOfWork.SaveChangesAsync(ct);
                    await _transactionManager.CommitAsync(ct);
                    return new InventoryOrderResultDto(request.OrderId, "UNRESERVED", _clock.UtcNow);
                }

                var storedBySku = storedReservations.ToDictionary(r => r.SkuId, r => r.Quantity);

                // Тело ДОЛЖНО соответствовать сохранённым резервам:
                //  - каждый item из тела имеет запись в БД
                //  - quantity из тела совпадает с сохранённым
                //  - в БД нет резервов, не упомянутых в теле (полное снятие)
                foreach (var (skuId, qty) in requestedByItems)
                {
                    if (!storedBySku.TryGetValue(skuId, out var storedQty))
                        throw new DomainException(
                            $"SKU {skuId} is not reserved under order {request.OrderId}",
                            "INVALID_REQUEST");
                    if (storedQty != qty)
                        throw new DomainException(
                            $"Reservation mismatch for SKU {skuId}: requested {qty}, stored {storedQty}",
                            "INVALID_REQUEST");
                }
                var missingInBody = storedBySku.Keys.Except(requestedByItems.Keys).ToList();
                if (missingInBody.Count > 0)
                    throw new DomainException(
                        $"Request must cover all reserved items of order. Missing: {string.Join(", ", missingInBody)}",
                        "INVALID_REQUEST");

                // Лочим SKU и применяем unreserve по СОХРАНЁННЫМ количествам, не по телу.
                // Тело уже проверено выше, но списываем из БД — защита от ошибки сравнения.
                var skuIds = storedBySku.Keys.ToList();
                var skus = await _skuRepository.GetByIdsForUpdateAsync(skuIds, ct);
                var skuMap = skus.ToDictionary(s => s.Id);

                foreach (var (skuId, storedQty) in storedBySku)
                {
                    if (!skuMap.TryGetValue(skuId, out var sku))
                        // SKU удалили после reserve — для unreserve это аномалия,
                        // но "молчаливо пропустить" безопаснее, чем падать на компенсации.
                        continue;
                    sku.UnReserve(storedQty);
                }

                // Удаляем записи резерва — резерв снят.
                _reservationRepository.RemoveRange(storedReservations);

                _idempotencyStore.Register(
                    key, messageType: "inventory.unreserve", source: "b2c",
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