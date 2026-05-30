using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.HomePage.Dtos;
using MediatR;

namespace B2C.Application.HomePage.Queries.ListCollections
{
    /// <summary>
    /// Список активных подборок БЕЗ товаров внутри.
    /// Public endpoint.
    /// </summary>
    public sealed record ListCollectionsQuery : IRequest<IReadOnlyList<CollectionSummaryDto>>;
}
