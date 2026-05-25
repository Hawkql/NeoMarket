using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using B2B.Application.Common.Interface;
using B2B.Application.Inventory.Dtos;
using B2B.Domain.Common;
using B2B.Domain.Skus;
using MediatR;

namespace B2B.Application.Inventory.Commands.Reserve
{
    public sealed class ReserveInventoryCommandHandler
    : IRequestHandler<ReserveInventoryCommand, ReserveResultDto>
    {
        private readonly ISkuRepository _skuRepository;
        private readonly IIdempotencyStore _idempotencyStore;
        private readonly ITransactionManager _transactionManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _clock;

        public ReserveInventoryCommandHandler(
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

        public async Task<ReserveResultDto> Handle(
            ReserveInventoryCommand request,
            CancellationToken ct)
        {
            // 1. Идемпотентность: если ключ уже обработан — возвращаем 200 без действий
            //    (idempotent_reserve_returns_200_without_double_deduction)
            if (await _idempotencyStore.ExistsAsync(request.IdempotencyKey, ct))
            {
                return new ReserveResultDto(
                    request.OrderId, "RESERVED", _clock.UtcNow);
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

                // Проверяем ВСЕ позиции до изменений (all-or-nothing)
                var problems = new List<string>();
                foreach (var (skuId, qty) in requested)
                {
                    if (!skuMap.TryGetValue(skuId, out var sku) || sku.Deleted)
                    {
                        problems.Add($"{skuId}: not found");
                        continue;
                    }
                    if (sku.ActiveQuantity < qty)
                    {
                        problems.Add(
                            $"{skuId}: insufficient stock " +
                            $"(have {sku.ActiveQuantity}, need {qty})");
                    }
                }

                if (problems.Count > 0)
                {
                    await _transactionManager.RollbackAsync(ct);
                    throw new DomainException(
                        $"Cannot reserve: {string.Join("; ", problems)}",
                        "INSUFFICIENT_STOCK");
                }

                // Применяем — по агрегированному количеству, один Reserve на SKU
                foreach (var (skuId, qty) in requested)
                {
                    skuMap[skuId].Reserve(qty);
                }

                _idempotencyStore.Register(
                    request.IdempotencyKey,
                    messageType: "inventory.reserve",
                    source: "b2c",
                    payload: JsonSerializer.Serialize(new
                    {
                        request.OrderId,
                        items = request.Items
                    }));

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
