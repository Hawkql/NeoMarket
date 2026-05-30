using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.HomePage.Dtos;
using B2C.Domain.HomePage;
using MediatR;

namespace B2C.Application.HomePage.Queries.ListCollections
{
    public sealed class ListCollectionsQueryHandler
        : IRequestHandler<ListCollectionsQuery, IReadOnlyList<CollectionSummaryDto>>
    {
        private readonly ICollectionRepository _collectionRepository;

        public ListCollectionsQueryHandler(ICollectionRepository collectionRepository)
        {
            _collectionRepository = collectionRepository;
        }

        public async Task<IReadOnlyList<CollectionSummaryDto>> Handle(
            ListCollectionsQuery request, CancellationToken ct)
        {
            var collections = await _collectionRepository.ListActiveAsync(ct);
            return collections.Select(HomePageMapper.ToCollectionSummary).ToList();
        }
    }
}
