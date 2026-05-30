using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Integration.Dtos
{
    /// <summary>
    /// Узел дерева категорий (US-CAT-05).
    /// Children — рекурсивно вложенные дочерние категории.
    /// </summary>
    public sealed record CategoryNode(
        Guid Id,
        Guid? ParentId,
        string Name,
        string Slug,
        IReadOnlyList<CategoryNode> Children);
}
