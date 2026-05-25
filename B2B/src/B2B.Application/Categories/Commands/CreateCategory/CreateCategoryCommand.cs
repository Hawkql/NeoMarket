using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Categories.Dtos;
using MediatR;

namespace B2B.Application.Categories.Commands.CreateCategory
{
    public sealed record CreateCategoryCommand(
        string Name,
        Guid? ParentId
    ) : IRequest<CategoryWithChildrenDto>;
}
