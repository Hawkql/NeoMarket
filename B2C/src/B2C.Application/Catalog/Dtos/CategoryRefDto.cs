using System;
using System.Collections.Generic;

namespace B2C.Application.Catalog.Dtos
{
    /// <summary>
    /// openapi: CategoryRef. required: id, name, level, path.
    /// path — массив имён от корня до текущей категории (включительно),
    /// используется фронтом для breadcrumbs без отдельного запроса.
    /// </summary>
    public sealed record CategoryRefDto(
        Guid Id,
        string Name,
        Guid? ParentId,
        int Level,
        IReadOnlyList<string> Path);
}