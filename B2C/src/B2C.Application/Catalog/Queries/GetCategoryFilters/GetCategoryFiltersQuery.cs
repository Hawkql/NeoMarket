using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;
using MediatR;

namespace B2C.Application.Catalog.Queries.GetCategoryFilters
{
    public sealed record GetCategoryFiltersQuery(Guid CategoryId) : IRequest<CategoryFiltersDto>;
}
