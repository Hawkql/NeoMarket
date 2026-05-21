using B2B.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2B.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping для агрегата Category (Adjacency List + самоссылка).
/// 
/// Особенности:
/// - parent_id — самоссылка на ту же таблицу categories (nullable для корневых)
/// - FK constraint на parent_id с DeleteBehavior.Restrict — БД не даст удалить
///   категорию с детьми (defense-in-depth)
/// - Composite covering index (parent_id, ordering) для эффективных запросов
///   "дети категории" и "корневые категории" с сортировкой
/// - Partial index по deleted = false для каталога
/// 
/// Навигационных свойств Parent / Children нет — между Category-агрегатами
/// связь только через Id (Reference by Identity). Построение дерева делается
/// в Application слое из плоского списка или через recursive CTE.
/// </summary>
public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // ====================================================================
        // 1. ТАБЛИЦА И PRIMARY KEY
        // ====================================================================
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // ====================================================================
        // 2. САМОССЫЛКА: ParentId
        // ====================================================================
        // Nullable — null означает корневую категорию.
        builder.Property(c => c.ParentId)
            .HasColumnName("parent_id");

        // FK constraint на самоссылку:
        // - WithMany() без аргумента — EF не создаёт навигацию Children
        // - HasForeignKey(c => c.ParentId) — типизированный FK через Domain свойство
        // - IsRequired(false) — FK nullable (для корневых категорий)
        // - OnDelete(Restrict) — БД не даст удалить категорию с детьми
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // ====================================================================
        // 3. КОНТЕНТ
        // ====================================================================
        builder.Property(c => c.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Ordering)
            .HasColumnName("ordering")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.Deleted)
            .HasColumnName("deleted")
            .IsRequired()
            .HasDefaultValue(false);

        // ====================================================================
        // 4. AUDIT
        // ====================================================================
        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        // ====================================================================
        // 5. DOMAIN EVENTS - игнорируем
        // ====================================================================
        builder.Ignore(c => c.DomainEvents);

        // ====================================================================
        // 6. ИНДЕКСЫ
        // ====================================================================
        // Основной composite covering index:
        //   - "все корневые категории отсортированные по ordering" (parent_id IS NULL)
        //   - "все дети категории X отсортированные по ordering" (parent_id = X)
        // 
        // Postgres использует этот индекс и для условия, и для сортировки.
        builder.HasIndex(c => new { c.ParentId, c.Ordering })
            .HasDatabaseName("ix_categories_parent_ordering");

        // Partial index — только активные категории.
        // Используется для каталога B2C, где удалённые не показываются.
        builder.HasIndex(c => c.ParentId)
            .HasDatabaseName("ix_categories_parent_active")
            .HasFilter("deleted = false");

        // ====================================================================
        // 7. CHECK CONSTRAINTS
        // ====================================================================
        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_categories_ordering_non_negative",
                "ordering >= 0");

            // Защита от self-loop "категория сама себе родитель"
            // (на уровне Domain это проверка в MoveTo, тут — defense-in-depth)
            t.HasCheckConstraint(
                "ck_categories_no_self_parent",
                "parent_id IS NULL OR parent_id <> id");
        });
    }
}