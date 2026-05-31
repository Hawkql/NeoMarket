using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Common.Abstractions
{
    public interface ICurrentUserService
    {
        /// <summary>Идентификатор пользователя (sub из JWT). Универсальный — для seller и admin.</summary>
        Guid UserId { get; }

        /// <summary>Алиас для UserId с семантикой "seller". Совпадает с UserId для seller-роли.</summary>
        Guid SellerId { get; }

        string Role { get; }
        bool IsAuthenticated { get; }
    }
}
