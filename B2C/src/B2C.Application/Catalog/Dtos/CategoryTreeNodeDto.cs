using System;
using System.Collections.Generic;

namespace B2C.Application.Catalog.Dtos
{
    /// <summary>
    /// openapi: CategoryTreeNode = CategoryRef + children.
    /// Плоская структура: повторяем поля CategoryRef + добавляем children.
    /// </summary>
    public sealed record CategoryTreeNodeDto(
        Guid Id,
        string Name,
        Guid? ParentId,
        int Level,
        IReadOnlyList<string> Path,
        IReadOnlyList<CategoryTreeNodeDto> Children);
}