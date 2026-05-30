using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Infrastructure.Outbox.Dispatchers
{
    /// <summary>
    /// Настройки HTTP-связи B2C → B2B. Привязываются из конфигурации (секция "B2BClient").
    /// 
    /// ServiceKey — ключ межсервисной аутентификации (заголовок X-Service-Key).
    /// Это НЕ JWT покупателя — отдельный механизм для server-to-server вызовов.
    /// 
    /// Используется и Outbox-диспетчером (исходящие события), и HTTP-клиентами
    /// IB2BCatalogClient/IB2BReservationClient (синхронные вызовы) — общие опции.
    /// </summary>
    public sealed class B2BClientOptions
    {
        public const string SectionName = "B2BClient";

        public string BaseUrl { get; set; } = null!;
        public string ServiceKey { get; set; } = null!;
    }
}
