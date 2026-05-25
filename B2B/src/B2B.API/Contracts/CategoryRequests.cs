using System.Text.Json.Serialization;

namespace B2B.Api.Contracts
{
    public sealed record CreateCategoryRequest(
    string Name,
    Guid? ParentId);

    // Для PATCH с различением "не передан" vs "null" используем класс с трекингом
    public sealed class UpdateCategoryRequest
    {
        public string? Name { get; set; }

        public Guid? ParentId { get; set; }

        // Флаг проставляется в сеттере: если JSON содержал parent_id (даже null) —
        // сеттер вызовется, флаг станет true. Если поля не было — сеттер не вызовется.
        [JsonIgnore]
        public bool ParentIdSpecified { get; private set; }

        [JsonPropertyName("parent_id")]
        public Guid? ParentIdValue
        {
            get => ParentId;
            set { ParentId = value; ParentIdSpecified = true; }
        }

        public bool? IsActive { get; set; }
    }
}
