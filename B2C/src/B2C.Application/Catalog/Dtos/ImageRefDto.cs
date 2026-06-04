
namespace B2C.Application.Catalog.Dtos
{
    /// <summary>
    /// openapi ImageRef. required: id, url, ordering.
    /// На MVP B2B не отдаёт id/ordering — генерим из URL-хеша и порядка в массиве.
    /// </summary>
    public sealed record ImageRefDto(Guid Id, string Url, int Ordering, string? Alt);
}