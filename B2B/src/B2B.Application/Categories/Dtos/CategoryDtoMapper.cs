using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Categories;

namespace B2B.Application.Categories.Dtos
{
    internal static class CategoryDtoMapper
    {
        public static CategoryDto Map(
            Category c,
            IReadOnlyDictionary<Guid, (int Level, string Path)> levelPath)
        {
            var (level, path) = levelPath.TryGetValue(c.Id, out var lp)
                ? lp
                : (0, c.Name.ToLowerInvariant());

            return new CategoryDto(
                Id: c.Id,
                Name: c.Name,
                ParentId: c.ParentId,
                Level: level,
                Path: path,
                IsActive: !c.Deleted,
                CreatedAt: c.CreatedAt);
        }
    }
}
