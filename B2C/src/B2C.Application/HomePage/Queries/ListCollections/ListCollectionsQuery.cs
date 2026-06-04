using System.Collections.Generic;
using B2C.Application.HomePage.Dtos;
using MediatR;

namespace B2C.Application.HomePage.Queries.ListCollections
{
    /// <summary>
    /// openapi: GET /api/v1/catalog/collections — массив Collection[]
    /// (каждая уже содержит обогащённые products).
    /// </summary>
    public sealed record ListCollectionsQuery : IRequest<IReadOnlyList<CollectionDetailDto>>;
}