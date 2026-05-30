using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Catalog.Dtos
{
    public sealed record CategoryTreeNodeDto(
        Guid Id,
        Guid? ParentId,
        string Name,
        string Slug,
        IReadOnlyList<CategoryTreeNodeDto> Children);
}
