using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;
using MediatR;

namespace B2C.Application.Catalog.Queries.GetBreadcrumbs
{
    /// <summary>
    /// Хлебные крошки от корня. Один из двух параметров должен быть заполнен, не оба.
    /// </summary>
    public sealed record GetBreadcrumbsQuery(
        Guid? CategoryId,
        Guid? ProductId) : IRequest<IReadOnlyList<BreadcrumbDto>>;
}
