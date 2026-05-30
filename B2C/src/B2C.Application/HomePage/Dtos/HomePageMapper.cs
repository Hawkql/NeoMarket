using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.HomePage;

namespace B2C.Application.HomePage.Dtos
{
    internal static class HomePageMapper
    {
        public static BannerDto ToBannerDto(Banner b) =>
            new(b.Id, b.Title, b.ImageUrl, b.LinkUrl, b.Priority);

        public static CollectionSummaryDto ToCollectionSummary(Collection c) =>
            new(c.Id,
                c.Slug,
                c.Title,
                c.Description,
                c.CoverImageUrl,
                c.ProductIds.Count);
    }
}
