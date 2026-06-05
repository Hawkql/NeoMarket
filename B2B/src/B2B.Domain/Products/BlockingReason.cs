using B2B.Domain.Common;

namespace B2B.Domain.Products
{
    /// <summary>
    /// Owned Value Object. Хранится в колонках product.blocking_reason_reason_id,
    /// blocking_reason_title, blocking_reason_comment.
    /// 
    /// Title приходит вместе с событием от Moderation (поле blocking_reason_title
    /// в ModerationEventRequest — необязательное расширение OpenAPI-схемы).
    /// Хранение title на стороне B2B нужно, чтобы карточка товара показывала
    /// человекочитаемую причину блокировки без обращения к Moderation
    /// при каждом чтении.
    /// </summary>
    public class BlockingReason
    {
        public Guid ReasonId { get; set; }
        public string? Title { get; set; }
        public string? Comment { get; set; }

        public BlockingReason() { }

        public BlockingReason(Guid reasonId, string? title, string? comment)
        {
            if (reasonId == Guid.Empty)
                throw new DomainException("BlockingReason id is required", "INVALID_REQUEST");

            ReasonId = reasonId;
            Title = title;
            Comment = comment;
        }
    }
}