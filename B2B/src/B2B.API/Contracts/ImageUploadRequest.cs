using Microsoft.AspNetCore.Mvc;

namespace B2B.Api.Contracts
{
    public sealed class ImageUploadRequest
    {
        public IFormFile File { get; set; } = null!;

        [FromForm(Name = "entity_type")]
        public string EntityType { get; set; } = null!;

        [FromForm(Name = "entity_id")]
        public Guid? EntityId { get; set; }

        public int Ordering { get; set; } = 0;
    }
}
