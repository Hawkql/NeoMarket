using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Infrastructure.Integration.Contracts
{
    /// <summary>
    /// SKU в ответе B2B API. Формат точно повторяет JSON B2B (snake_case при десериализации
    /// настроен глобально). Содержит ВСЕ поля, что отдаёт B2B — включая те, что мы НЕ
    /// прокидываем дальше (это и есть граница ACL: фильтрация здесь, в маппинге клиента).
    /// </summary>
    public sealed class B2bSkuResponse
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string Name { get; set; } = null!;
        public int Price { get; set; }              // копейки
        public int Discount { get; set; }
        public string? ImageUrl { get; set; }
        public int ActiveQuantity { get; set; }     // ← НЕ прокидываем наружу (внутренняя инфа)
        public List<B2bCharacteristicResponse> Characteristics { get; set; } = new();
    }
}
