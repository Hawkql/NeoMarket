using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Categories.Dtos;
using MediatR;

namespace B2B.Application.Categories.Commands.UpdateCategory
{
    public sealed record UpdateCategoryCommand(
        Guid CategoryId,
        string? Name,
        Guid? ParentId,
        bool ParentIdSpecified,   // отличить "parent_id не передан" от "parent_id: null"
        bool? IsActive
    ) : IRequest<CategoryWithChildrenDto>;
}
