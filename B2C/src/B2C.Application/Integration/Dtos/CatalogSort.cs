namespace B2C.Application.Integration.Dtos
{
    /// <summary>
    /// Внутренний (B2B) контракт сортировки. Публичный API использует CatalogSortDto
    /// (только Popularity/PriceAsc/PriceDesc/New по openapi). Здесь оставляем расширенный
    /// набор для совместимости со старым B2B-протоколом.
    /// </summary>
    public enum CatalogSort
    {
        Popularity = 0,
        PriceAsc = 1,
        PriceDesc = 2,
        New = 3,
        Rating = 4,
        DateDesc = 5,
        DiscountDesc = 6,
    }
}