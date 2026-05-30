using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Integration.Dtos
{
    /// <summary>Один элемент цепочки хлебных крошек (US-CAT-05).</summary>
    public sealed record Breadcrumb(Guid CategoryId, string Name, string Slug);
}
