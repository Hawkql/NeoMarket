using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.HomePage.Dtos
{
    /// <summary>Баннер на главной странице. Возвращается из GET /home/banners.</summary>
    public sealed record BannerDto(
        Guid Id,
        string Title,
        string ImageUrl,
        string? LinkUrl,
        int Priority);
}
