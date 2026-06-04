using System;
using B2C.Application.HomePage.Dtos;
using MediatR;

namespace B2C.Application.HomePage.Queries.GetCollection
{

    public sealed record GetCollectionQuery(Guid Id) : IRequest<CollectionDetailDto>;
}