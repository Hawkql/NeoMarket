using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;
using B2B.Domain.Products.Events;

namespace B2B.Domain.Products
{
    // B2B.Domain/Products/Product.cs
    public class Product : AggregateRoot<Guid>, IAuditableEntity
    {

        private readonly List<ProductCharacteristic> _characteristics = new();
        private readonly List<FieldReport> _fieldReports = new();

        // identity
        public Guid SellerId { get; private set; }
        public Guid CategoryId { get; private set; }

        // content
        public string Title { get; private set; } = null!;
        public string Description { get; private set; } = null!;
        public string Slug { get; private set; } = null!;
        public IReadOnlyCollection<ProductCharacteristic> Characteristics => _characteristics.AsReadOnly();

        //lifecycle
        public ProductStatus Status { get; private set; }
        public bool Deleted { get; private set; }

        public bool Blocked => Status is ProductStatus.Blocked or ProductStatus.HardBlocked;

        public BlockingReason? BlockingReason { get; private set; }
        public IReadOnlyCollection<FieldReport> FieldReports => _fieldReports.AsReadOnly();

        /// <summary>
        /// Номер текущего раунда модерации. 0 — товар никогда не был на модерации.
        /// Инкрементируется при каждом отправлении на модерацию.
        /// </summary>
        public int ModerationRound { get; private set; }

        //audit

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        private Product() { }
        private Product(Guid id, Guid sellerId, Guid categoryId,
        string title, string description) : base(id)
        {

            Title = title;
            Description = description ?? string.Empty;
            Slug = GenerateSlug(title);
            CategoryId = categoryId;
            SellerId = sellerId;
            Status = ProductStatus.Created;
            Deleted = false;
            ModerationRound = 0;
        }

        public static Product Create(
            Guid sellerId,
             Guid categoryId,
            string title,
            string description,
            IEnumerable<ProductCharacteristic>? characteristics = null
            )
        {
            if (sellerId == Guid.Empty)
                throw new DomainException("SellerId is required", "INVALID_REQUEST");
            if (categoryId == Guid.Empty)
                throw new DomainException("CategoryId is required", "INVALID_REQUEST");

            ValidateContent(title, description);

            var product = new Product(Guid.NewGuid(), sellerId, categoryId, title, description);
            if (characteristics is not null)
                product._characteristics.AddRange(characteristics);

            product.RaiseDomainEvent(new ProductCreatedEvent(product.Id, sellerId));
            return product;
        }
        /// <summary>
        /// Повторная модерация после редактирования (товара или его SKU).
        /// Срабатывает только из MODERATED/BLOCKED — из CREATED/ON_MODERATION/HARD_BLOCKED
        /// переход не нужен или невозможен. Поднимает событие EDITED.
        /// Используется при PATCH /skus/{id} (US-B2B-03).
        /// </summary>
        public void SendToModerationOnEdit()
        {
            EnsureCanBeEdited();   // HARD_BLOCKED/deleted → FORBIDDEN
            if (Status is ProductStatus.Moderated or ProductStatus.Blocked)
                SentToModeration(ModerationReason.Edited);
        }
        /// <summary>
        /// Удалён последний SKU у товара на модерации → возврат в CREATED
        /// (нет SKU = модерация не нужна). US-12.
        /// </summary>
        public void RevertToCreatedOnLastSkuRemoved()
        {
            if (Status == ProductStatus.OnModeration)
                Status = ProductStatus.Created;
        }

        /// <summary>
        /// Проверка перед удалением SKU: HARD_BLOCKED запрещает (US-12).
        /// </summary>
        public void EnsureCanDeleteSku()
        {
            if (Status == ProductStatus.HardBlocked)
                throw new DomainException(
                    "Cannot delete SKU of hard-blocked product", "FORBIDDEN");
        }
        /// <summary>
        /// Проверка, что товар не удалён и не HARD_BLOCKED — операции редактирования
        /// над таким товаром запрещены.
        /// </summary>
        public void EnsureCanBeEdited()
        {
            if (Deleted)
                throw new DomainException("Product is deleted", "FORBIDDEN");
            if (Status == ProductStatus.HardBlocked)
                throw new DomainException("Cannot edit hard-blocked product", "FORBIDDEN");
        }
        private static string GenerateSlug(string title)
        {
            var baseSlug = new string((title ?? string.Empty).ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '-')
                .ToArray())
                .Trim('-');

            while (baseSlug.Contains("--"))
                baseSlug = baseSlug.Replace("--", "-");

            var suffix = Guid.NewGuid().ToString("N")[..6];
            return string.IsNullOrEmpty(baseSlug) ? suffix : $"{baseSlug}-{suffix}";
        }
        /// <summary>Проверка, можно ли добавлять SKU (для CreateSku Handler).</summary>
        public void EnsureCanAddSku()
        {
            if (Deleted)
                throw new DomainException("Product is deleted", "FORBIDDEN");
            if (Status == ProductStatus.HardBlocked)
                throw new DomainException(
                    "Cannot add SKU to hard-blocked product", "FORBIDDEN");
        }
        /// <summary>
        /// Редактирование контента товара. Если товар был MODERATED или BLOCKED —
        /// автоматически отправляется на повторную модерацию.
        /// </summary>
        public void Update(Guid categoryId,
            string title,
            string description,
            IEnumerable<ProductCharacteristic>? characteristics = null)
        {
            EnsureCanBeEdited();
            ValidateContent(title, description);
            if (categoryId == Guid.Empty)
                throw new DomainException("CategoryId is required", "INVALID_REQUEST");


            CategoryId = categoryId;
            Title = title;
            Description = description;

            if (characteristics is not null)
            {
                _characteristics.Clear();
                _characteristics.AddRange(characteristics);
            }

            RaiseDomainEvent(new ProductUpdatedEvent(Id, SellerId));

            // Автоматический перевод на повторную модерацию
            if (Status is ProductStatus.Moderated or ProductStatus.Blocked)
                SentToModeration(ModerationReason.Edited);
        }



        //Отправка на модерацию

        public void SendToModerationOnFirstSku()
        {
            if (Status != ProductStatus.Created)
                return;
            SentToModeration(ModerationReason.FirstSkuAdded);
        }

        private void SentToModeration(ModerationReason reason)
        {
            Status = ProductStatus.OnModeration;
            ModerationRound++;
            RaiseDomainEvent(new ProductSentToModerationEvent(Id, SellerId, reason));
        }

        /// <summary>
        /// ON_MODERATION → MODERATED. Вызывается из обработчика входящего события
        /// от Moderation (B2B-9).
        /// </summary>
        public void Approve()
        {
            if (Status != ProductStatus.OnModeration)
                throw new DomainException($"Cannot approve product in status {Status}");
            Status = ProductStatus.Moderated;
            BlockingReason = null;
            RaiseDomainEvent(new ProductApprovedEvent(Id));
        }
        /// <summary>
        /// ON_MODERATION → BLOCKED. Мягкая блокировка с возможностью исправить.
        /// </summary>
        public void Block(BlockingReason reason,
            IEnumerable<(FieldReportTarget Field, Guid? skuId, string comment)> reports,
            IEnumerable<Guid> skuIds,
            DateTime now)
        {
            if (Status != ProductStatus.OnModeration)
                throw new DomainException(
                    $"Cannot block product in status {Status}",
                    "INVALID_STATE_TRANSITION");
            if (reason is null)
                throw new DomainException("BlockingReason is required", "INVALID_REQUEST");
            Status = ProductStatus.Blocked;
            BlockingReason = reason;
            AddFieldReports(reports, now);
            RaiseDomainEvent(new ProductBlockedEvent(Id, skuIds.ToList()));
        }
        /// <summary>
        /// → HARD_BLOCKED. Жёсткая блокировка, terminal state.
        /// Может вызываться из любого статуса (модератор или админ).
        /// </summary>
        public void HardBlock(
         BlockingReason reason,
         IEnumerable<(FieldReportTarget Field, Guid? SkuId, string Comment)> reports,
         IEnumerable<Guid> skuIds,
         DateTime now)
        {
            if (Status == ProductStatus.HardBlocked)
                throw new DomainException("Product is already hard-blocked", "INVALID_REQUEST");
            if (reason is null)
                throw new DomainException("BlockingReason is required", "INVALID_REQUEST");
            Status = ProductStatus.HardBlocked;
            BlockingReason = reason;
            AddFieldReports(reports, now);
            RaiseDomainEvent(new ProductHardBlockedEvent(Id, skuIds.ToList()));
        }
        public void MarkAsDeleted(IEnumerable<Guid> skuIds)
        {
            if (Deleted)
                throw new DomainException("Product already deleted", "INVALID_REQUEST");
            Deleted = true;
            RaiseDomainEvent(new ProductDeletedEvent(Id, SellerId, skuIds.ToList()));
        }

        private void AddFieldReports(IEnumerable<(FieldReportTarget Field, Guid? SkuId, string Comment)> reports, DateTime now)
        {
            foreach (var (field, skuId, comment) in reports)
            {
                var report = new FieldReport(
                Guid.NewGuid(),
                Id,
                field,
                skuId,
                comment,
                ModerationRound,  // привязываем к текущему раунду
                now);
                _fieldReports.Add(report);
            }
        }

        private static void ValidateContent(string title, string description)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("title is required", "INVALID_REQUEST");
            if (title.Length > 255)
                throw new DomainException("title must be 1-255 characters", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(description))
                throw new DomainException("description is required", "INVALID_REQUEST");
            if (description.Length > 5000)
                throw new DomainException("description must be 1-5000 characters", "INVALID_REQUEST");
        }
    }
}
