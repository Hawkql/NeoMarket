using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;
using B2C.Application.HomePage.Dtos;
using B2C.Application.Integration;
using B2C.Domain.Common;
using B2C.Domain.HomePage;
using MediatR;

namespace B2C.Application.HomePage.Queries.GetCollection
{
    /// <summary>
    /// Шаги:
    ///   1. Найти подборку по Id (репозиторий должен иметь GetByIdAsync).
    ///   2. Если ProductIds пустой — вернуть пустой products.
    ///   3. Batch enrichment через B2B, сохраняя порядок куратора.
    ///   4. Удалённые/заблокированные товары — silent skip.
    /// </summary>
    public sealed class GetCollectionQueryHandler
        : IRequestHandler<GetCollectionQuery, CollectionDetailDto>
    {
        private readonly ICollectionRepository _collectionRepository;
        private readonly IB2BCatalogClient _b2bCatalog;

        public GetCollectionQueryHandler(
            ICollectionRepository collectionRepository,
            IB2BCatalogClient b2bCatalog)
        {
            _collectionRepository = collectionRepository;
            _b2bCatalog = b2bCatalog;
        }

        public async Task<CollectionDetailDto> Handle(
            GetCollectionQuery request, CancellationToken ct)
        {
            var collection = await _collectionRepository.GetByIdAsync(request.Id, ct)
                ?? throw new DomainException("Collection not found", "NOT_FOUND");

            if (collection.ProductIds.Count == 0)
                return new CollectionDetailDto(
                    Id: collection.Id,
                    Name: collection.Title,
                    Description: collection.Description,
                    Products: Array.Empty<CatalogProductCardDto>());

            var products = await _b2bCatalog.GetProductsBatchAsync(collection.ProductIds, ct);
            var productsById = products.ToDictionary(p => p.Id);

            // Сохраняем порядок ProductIds куратора.
            var ordered = collection.ProductIds
                .Where(productsById.ContainsKey)
                .Select(id => CatalogMapper.ToCard(productsById[id]))
                .ToList();

            return new CollectionDetailDto(
                Id: collection.Id,
                Name: collection.Title,
                Description: collection.Description,
                Products: ordered);
        }
    }
}