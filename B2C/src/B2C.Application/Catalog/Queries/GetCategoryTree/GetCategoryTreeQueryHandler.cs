using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;
using B2C.Application.Integration;
using B2C.Application.Integration.Dtos;
using B2C.Domain.Common;
using MediatR;

namespace B2C.Application.Catalog.Queries.GetCategoryTree
{
    /// <summary>
    /// US-CAT-05: дерево категорий + проверка целостности.
    /// Если в выдаче B2B обнаружена orphan-нода (ParentId указывает на узел,
    /// которого нет в этой же выдаче), бросаем 422.
    /// </summary>
    public sealed class GetCategoryTreeQueryHandler
       : IRequestHandler<GetCategoryTreeQuery, IReadOnlyList<CategoryTreeNodeDto>>
    {
        private readonly IB2BCatalogClient _b2bCatalog;

        public GetCategoryTreeQueryHandler(IB2BCatalogClient b2bCatalog)
        {
            _b2bCatalog = b2bCatalog;
        }

        public async Task<IReadOnlyList<CategoryTreeNodeDto>> Handle(
            GetCategoryTreeQuery request, CancellationToken ct)
        {
            var tree = await _b2bCatalog.GetCategoryTreeAsync(ct);

            EnsureNoOrphans(tree);

            return tree.Select(CatalogMapper.ToTreeNode).ToList();
        }

        private static void EnsureNoOrphans(IReadOnlyList<CategoryNode> roots)
        {
            var allIds = new HashSet<Guid>();
            CollectIds(roots, allIds);

            var orphan = FindOrphan(roots, allIds);
            if (orphan is not null)
                throw new DomainException(
                    $"Category tree is broken: node '{orphan.Name}' ({orphan.Id}) " +
                    $"references parent {orphan.ParentId} that does not exist",
                    "UNPROCESSABLE_ENTITY");
        }

        private static void CollectIds(IReadOnlyList<CategoryNode> nodes, HashSet<Guid> acc)
        {
            foreach (var n in nodes)
            {
                acc.Add(n.Id);
                if (n.Children is { Count: > 0 })
                    CollectIds(n.Children, acc);
            }
        }

        private static CategoryNode? FindOrphan(
            IReadOnlyList<CategoryNode> nodes, HashSet<Guid> allIds)
        {
            foreach (var n in nodes)
            {
                if (n.ParentId is not null && !allIds.Contains(n.ParentId.Value))
                    return n;

                if (n.Children is { Count: > 0 })
                {
                    var deeper = FindOrphan(n.Children, allIds);
                    if (deeper is not null) return deeper;
                }
            }
            return null;
        }
    }
}