using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Catalog.Dtos
{
    /// <summary>Один элемент цепочки хлебных крошек.</summary>
    public sealed record BreadcrumbDto(Guid CategoryId, string Name, string Slug);
}
