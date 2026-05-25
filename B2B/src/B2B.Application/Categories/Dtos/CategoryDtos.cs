using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Categories.Dtos
{
    // CategoryResponse — плоская карточка
    public sealed record CategoryDto(
        Guid Id,
        string Name,
        Guid? ParentId,
        int Level,
        string Path,
        bool IsActive,           // = !Deleted
        DateTime CreatedAt);

    // CategoryWithChildrenResponse — категория + прямые дети
    public sealed record CategoryWithChildrenDto(
        Guid Id,
        string Name,
        Guid? ParentId,
        int Level,
        string Path,
        bool IsActive,
        DateTime CreatedAt,
        IReadOnlyList<CategoryDto> Children);

    // CategoryTreeResponse — рекурсивное дерево (id, name, children)
    public sealed record CategoryTreeDto(
        Guid Id,
        string Name,
        IReadOnlyList<CategoryTreeDto> Children);
}
