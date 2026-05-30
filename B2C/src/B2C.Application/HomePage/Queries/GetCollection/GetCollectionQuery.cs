using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.HomePage.Dtos;
using MediatR;

namespace B2C.Application.HomePage.Queries.GetCollection
{
    /// <summary>
    /// Подборка по slug (slug-friendly URLs: /collections/popular-electronics).
    /// </summary>
    public sealed record GetCollectionQuery(string Slug) : IRequest<CollectionDetailDto>;
}
