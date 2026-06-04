using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;
using B2C.Application.Integration;
using MediatR;

namespace B2C.Application.Catalog.Queries.ListCategories
{
    /// <summary>
    /// B2B всё ещё отдаёт дерево (это его контракт), а нам нужен плоский список —
    /// разворачиваем здесь. EnsureNoOrphans вынесли бы в общее место, но пока
    /// дублируем минимально: для плоского list orphan не критичен (просто
    /// окажется в списке как есть), так что валидацию здесь не повторяем.
    /// </summary>
    public sealed class ListCategoriesQueryHandler
        : IRequestHandler<ListCategoriesQuery, IReadOnlyList<CategoryRefDto>>
    {
        private readonly IB2BCatalogClient _b2bCatalog;

        public ListCategoriesQueryHandler(IB2BCatalogClient b2bCatalog) =>
            _b2bCatalog = b2bCatalog;

        public async Task<IReadOnlyList<CategoryRefDto>> Handle(
            ListCategoriesQuery request, CancellationToken ct)
        {
            var tree = await _b2bCatalog.GetCategoryTreeAsync(ct);
            return CatalogMapper.ToFlatRefs(tree);
        }
    }
}