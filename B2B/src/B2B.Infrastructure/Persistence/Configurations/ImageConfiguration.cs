using B2B.Domain.Images;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2B.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping для агрегата Image (polymorphic association).
/// 
/// Особенности:
/// - Polymorphic owner через колонки entity_type + entity_id (нет FK на ORM-уровне)
/// - entity_type хранится как lower-case строка ('product', 'sku') для совместимости с API
/// - Composite covering index (entity_type, entity_id, ordering) для быстрого
///   получения галереи владельца с правильной сортировкой за один scan
/// - CHECK constraint на entity_type — защита от мусорных значений
/// 
/// Целостность данных (что entity_id существует) — ответственность Application
/// Handler'ов, а не БД. При удалении Product/Sku Handler должен явно удалить
/// связанные Images.
/// </summary>
public sealed class ImageConfiguration : IEntityTypeConfiguration<Image>
{
    public void Configure(EntityTypeBuilder<Image> builder)
    {
        // ====================================================================
        // 1. ТАБЛИЦА И PRIMARY KEY
        // ====================================================================
        builder.ToTable("images");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // ====================================================================
        // 2. POLYMORPHIC OWNER
        // ====================================================================
        // entity_type — храним как строку нижнего регистра.
        // Это НЕ HasConversion<string>() — стандартный конвертер выдаст
        // "Product" (PascalCase). Нам нужно "product" — кастомный конвертер.
        builder.Property(i => i.EntityType)
            .HasColumnName("entity_type")
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<ImageEntityType>(v, true))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.EntityId)
            .HasColumnName("entity_id")
            .IsRequired();

        // ====================================================================
        // 3. КОНТЕНТ
        // ====================================================================
        builder.Property(i => i.Url)
            .HasColumnName("url")
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(i => i.Ordering)
            .HasColumnName("ordering")
            .IsRequired()
            .HasDefaultValue(0);

        // ====================================================================
        // 4. AUDIT
        // ====================================================================
        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        // ====================================================================
        // 5. DOMAIN EVENTS - игнорируем
        // ====================================================================
        builder.Ignore(i => i.DomainEvents);

        // ====================================================================
        // 6. ИНДЕКСЫ
        // ====================================================================
        // Composite covering index для основного запроса:
        //   "получить все картинки entity, отсортированные по ordering"
        // 
        // Postgres может вернуть результат уже отсортированным, не делая
        // отдельный sort. Это критично для производительности при загрузке
        // карточек товаров.
        builder.HasIndex(i => new { i.EntityType, i.EntityId, i.Ordering })
            .HasDatabaseName("ix_images_owner");

        // ====================================================================
        // 7. CHECK CONSTRAINTS
        // ====================================================================
        builder.ToTable(t =>
        {
            // Защита от мусорных значений entity_type, если кто-то пишет SQL мимо EF
            t.HasCheckConstraint(
                "ck_images_entity_type_valid",
                "entity_type IN ('product', 'sku')");

            // Ordering >= 0
            t.HasCheckConstraint(
                "ck_images_ordering_non_negative",
                "ordering >= 0");
        });
    }
}