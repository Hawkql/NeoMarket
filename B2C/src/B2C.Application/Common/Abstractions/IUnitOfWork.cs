using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Common.Abstractions
{
    /// <summary>
    /// Unit of Work (Fowler) — единый коммит всех изменений в репозиториях.
    /// Реализация в Infrastructure — поверх DbContext.SaveChangesAsync.
    /// 
    /// В Domain слое нет SaveChanges на репозиториях: AddAsync только трекает изменения,
    /// фактический коммит делает Application через IUnitOfWork.SaveChangesAsync.
    /// </summary>
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct);
    }
}
