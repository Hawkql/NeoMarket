using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Domain.Products.Events
{
    public  enum ModerationReason
    {
        /// <summary>Первый SKU добавлен у CREATED товара.</summary>
        FirstSkuAdded,
        /// <summary>Товар отредактирован после одобрения/блокировки.</summary>
        Edited
    }
}
