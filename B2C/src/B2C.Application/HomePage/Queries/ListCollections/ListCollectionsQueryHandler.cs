using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;
using B2C.Application.HomePage.Dtos;
using B2C.Application.Integration;
using B2C.Domain.HomePage;
using MediatR;

namespace B2C.Application.HomePage.Queries.ListCollections
{
    /// <summary>
    /// openapi требует products уже в составе Collection — обогащаем все подборки
    /// за один batch-запрос в B2B (объединяя ProductIds всех активных подборок).
    /// 
    /// Порядок ProductIds внутри каждой подборки — значим (его задаёт куратор):
    /// после получения данных из B2B восстанавливаем порядок по позиции в исходном списке.
    /// 
    /// Удалённые/заблокированные товары — просто исчезают из products (silent skip).
    /// </summary>
    public sealed class ListCollectionsQueryHandler
        : IRequestHandler<ListCollectionsQuery, IReadOnlyList<CollectionDetailDto>>
    {
        private readonly ICollectionRepository _collectionRepository;
        private readonly IB2BCatalogClient _b2bCatalog;

        public ListCollectionsQueryHandler(
            ICollectionRepository collectionRepository,
            IB2BCatalogClient b2bCatalog)
        {
            _collectionRepository = collectionRepository;
            _b2bCatalog = b2bCatalog;
        }

        public async Task<IReadOnlyList<CollectionDetailDto>> Handle(
            ListCollectionsQuery request, CancellationToken ct)
        {
            var collections = await _collectionRepository.ListActiveAsync(ct);
            if (collections.Count == 0)
                return Array.Empty<CollectionDetailDto>();

            // Единый batch на все подборки — не делаем N запросов.
            var allProductIds = collections
                .SelectMany(c => c.ProductIds)
                .Distinct()
                .ToList();

            var productsById = allProductIds.Count == 0
                ? new Dictionary<Guid, Catalog.Dtos.CatalogProductCardDto>()
                : (await _b2bCatalog.GetProductsBatchAsync(allProductIds, ct))
                    .ToDictionary(p => p.Id, CatalogMapper.ToCard);

            // Сохраняем порядок ProductIds внутри каждой подборки.
            return collections.Select(c =>
            {
                var products = c.ProductIds
                    .Where(productsById.ContainsKey)
                    .Select(id => productsById[id])
                    .ToList();

                return new CollectionDetailDto(
                    Id: c.Id,
                    Name: c.Title,             // domain: Title → openapi: name
                    Description: c.Description,
                    Products: products);
            }).ToList();
        }
    }
}