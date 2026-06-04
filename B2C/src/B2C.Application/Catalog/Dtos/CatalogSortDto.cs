namespace B2C.Application.Catalog.Dtos
{
    /// <summary>
    /// API-уровневый sort. Значения соответствуют openapi enum:
    /// price_asc | price_desc | popularity | new.
    /// </summary>
    public enum CatalogSortDto
    {
        Popularity = 0,   // default по openapi
        PriceAsc = 1,
        PriceDesc = 2,
        New = 3,
    }
}