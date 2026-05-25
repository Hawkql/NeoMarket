using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Categories.Dtos;
using MediatR;

namespace B2B.Application.Categories.Queries.ListCategories
{
    public sealed record ListCategoriesQuery(
        Guid? ParentId,
        bool OnlyRoot
    ) : IRequest<IReadOnlyList<CategoryDto>>;
}
