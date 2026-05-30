using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Domain.HomePage
{
    public interface IBannerRepository
    {
        Task<Banner?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Видимые баннеры на момент now, отсортированные по Priority DESC.
        /// Условие: IsActive AND (StartsAt IS NULL OR StartsAt &lt;= now) AND (EndsAt IS NULL OR EndsAt &gt;= now).
        /// </summary>
        Task<IReadOnlyList<Banner>> ListVisibleAsync(DateTime now, CancellationToken ct = default);

        Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

        Task AddAsync(Banner banner, CancellationToken ct = default);
        void Update(Banner banner);
        void Remove(Banner banner);
    }
}
