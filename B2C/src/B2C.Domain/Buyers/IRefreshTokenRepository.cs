using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Domain.Buyers
{
    /// <summary>
    /// Отдельный репозиторий для refresh-токенов. 
    /// Прагматичное отступление от чистого DDD (по идее доступ должен идти через Buyer-агрегат),
    /// но при /auth/refresh нужен быстрый lookup по hash без загрузки всего Buyer'а.
    /// </summary>
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct);
        Task AddAsync(RefreshToken token, CancellationToken ct);

        /// <summary>
        /// Bulk-отзыв всех активных refresh-токенов покупателя.
        /// Используется при удалении аккаунта и при смене пароля.
        /// </summary>
        Task RevokeAllForBuyerAsync(Guid buyerId, CancellationToken ct);
    }
}
