using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.HomePage.Dtos;
using MediatR;

namespace B2C.Application.HomePage.Queries.GetActiveBanners
{

    /// <summary>
    /// Возвращает активные баннеры в порядке Priority DESC.
    /// Public endpoint: видим всем, в том числе гостям.
    /// </summary>
    public sealed record GetActiveBannersQuery : IRequest<IReadOnlyList<BannerDto>>;
}
