using B2B.Domain.Common;

namespace B2B.Domain.Products
{
    /// <summary>
    /// Owned Value Object. Хранится в колонках product.blocking_reason_reason_id
    /// и product.blocking_reason_comment. Title не хранится — по OpenAPI
    /// (ProductResponse, ModerationEventRequest) поля title нет; список причин —
    /// зона ответственности Moderation-сервиса.
    /// </summary>
    public class BlockingReason
    {
        public Guid ReasonId { get; set; }
        public string? Comment { get; set; }

        public BlockingReason() { }

        public BlockingReason(Guid reasonId, string? comment)
        {
            if (reasonId == Guid.Empty)
                throw new DomainException("BlockingReason id is required", "INVALID_REQUEST");

            ReasonId = reasonId;
            Comment = comment;
        }
    }
}