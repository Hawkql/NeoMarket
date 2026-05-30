using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;
using B2C.Application.HomePage.Dtos;
using B2C.Application.Integration;
using B2C.Domain.Common;
using B2C.Domain.HomePage;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2C.Application.HomePage.Queries.GetCollection
{
    /// <summary>
    /// Шаги:
    ///   1. Найти подборку по slug.
    ///   2. Если ProductIds пустой — вернуть пустой список товаров.
    ///   3. Batch-обогащение через IB2BCatalogClient (тот же паттерн, что в Favorites).
    ///   4. ВАЖНО: сохраняем порядок ProductIds из подборки (он значим — задаётся куратором).
    ///      B2B может вернуть товары в произвольном порядке, поэтому пересортируем
    ///      по позиции в исходном списке.
    ///   5. Используем CatalogMapper.ToCard — переиспользование маппера каталога.
    /// </summary>
    public sealed class GetCollectionQueryHandler
        : IRequestHandler<GetCollectionQuery, CollectionDetailDto>
    {
        private readonly ICollectionRepository _collectionRepository;
        private readonly IB2BCatalogClient _b2bCatalog;
        private readonly ILogger<GetCollectionQueryHandler> _logger;

        public GetCollectionQueryHandler(
            ICollectionRepository collectionRepository,
            IB2BCatalogClient b2bCatalog,
            ILogger<GetCollectionQueryHandler> logger)
        {
            _collectionRepository = collectionRepository;
            _b2bCatalog = b2bCatalog;
            _logger = logger;
        }

        public async Task<CollectionDetailDto> Handle(
            GetCollectionQuery request, CancellationToken ct)
        {
            var collection = await _collectionRepository.GetBySlugAsync(request.Slug, ct)
                ?? throw new DomainException("Collection not found", "NOT_FOUND");

            // Пустая подборка — без обращения к B2B.
            if (collection.ProductIds.Count == 0)
                return new CollectionDetailDto(
                    collection.Id, collection.Slug, collection.Title,
                    collection.Description, collection.CoverImageUrl,
                    Array.Empty<CatalogProductCardDto>(),
                    Array.Empty<Guid>());

            // Batch enrichment + сбор unavailable_ids (US-CART-05 acceptance).
            var products = await _b2bCatalog.GetProductsBatchAsync(collection.ProductIds, ct);
            var productsById = products.ToDictionary(p => p.Id);

            var ordered = new List<CatalogProductCardDto>(collection.ProductIds.Count);
            var unavailable = new List<Guid>();

            foreach (var productId in collection.ProductIds)
            {
                if (productsById.TryGetValue(productId, out var product))
                    ordered.Add(CatalogMapper.ToCard(product));
                else
                    unavailable.Add(productId);   // ← было silent skip + log
            }

            return new CollectionDetailDto(
                collection.Id,
                collection.Slug,
                collection.Title,
                collection.Description,
                collection.CoverImageUrl,
                ordered,
                unavailable);
        }
    }
    
}
