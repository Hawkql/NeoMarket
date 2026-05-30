using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Integration.Dtos
{
    /// <summary>
    /// Определение одного фильтра для категории. Slug — машинный ключ
    /// (для query parameter), Name — человекочитаемое имя.
    /// </summary>
    public sealed record FilterDefinition(
        string Slug,
        string Name,
        IReadOnlyList<FilterValue> Values);
}
