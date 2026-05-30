using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Carts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2C.Infrastructure.Persistence.Configurations
{
    public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
    {
        public void Configure(EntityTypeBuilder<Cart> builder)
        {
            builder.ToTable("carts");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

            builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
            builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

            builder.Ignore(c => c.DomainEvents);

            // ================================================================
            // CartOwner — Value Object (BuyerId XOR SessionId).
            // OwnsOne: поля VO ложатся в ту же таблицу carts (owner_buyer_id, owner_session_id).
            // EF требует, чтобы у Owned был доступ — Owner имеет приватный конструктор,
            // EF материализует через него.
            // ================================================================
            builder.OwnsOne(c => c.Owner, owner =>
            {
                owner.Property(o => o.BuyerId)
                    .HasColumnName("owner_buyer_id");   // nullable — для гостя null

                owner.Property(o => o.SessionId)
                    .HasColumnName("owner_session_id")
                    .HasMaxLength(128);                  // nullable — для авторизованного null

                // Индексы для поиска корзины по владельцу.
                // Partial unique: один buyer = одна корзина (среди не-null buyer_id).
                owner.HasIndex(o => o.BuyerId)
                    .IsUnique()
                    .HasDatabaseName("ux_carts_owner_buyer")
                    .HasFilter("owner_buyer_id IS NOT NULL");

                owner.HasIndex(o => o.SessionId)
                    .IsUnique()
                    .HasDatabaseName("ux_carts_owner_session")
                    .HasFilter("owner_session_id IS NOT NULL");
            });

            // Owner — обязателен (всегда задан через фабрику ForBuyer/ForGuest).
            builder.Navigation(c => c.Owner).IsRequired();

            // ================================================================
            // CartItem — внутренняя entity, OwnsMany в таблицу cart_items.
            // Доступ через backing field _items (публичное свойство Items readonly).
            // ================================================================
            builder.OwnsMany(c => c.Items, item =>
            {
                item.ToTable("cart_items");

                item.WithOwner().HasForeignKey("cart_id");

                item.HasKey(i => i.Id);
                item.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();
                item.Property(i => i.CartId).HasColumnName("cart_id");
                item.Property(i => i.SkuId).HasColumnName("sku_id").IsRequired();
                item.Property(i => i.ProductId).HasColumnName("product_id").IsRequired();
                item.Property(i => i.Quantity).HasColumnName("quantity").IsRequired();

                // UnavailableReason — status-enum, string по B2B-стилю
                // ("None", "ProductBlocked", "ProductDeleted", "OutOfStock").
                item.Property(i => i.UnavailableReason)
                    .HasColumnName("unavailable_reason")
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                item.Property(i => i.AddedAt).HasColumnName("added_at").HasColumnType("timestamptz").IsRequired();

                // Индексы для реакции на события от B2B (cross-cart по sku/product).
                item.HasIndex(i => i.SkuId).HasDatabaseName("ix_cart_items_sku");
                item.HasIndex(i => i.ProductId).HasDatabaseName("ix_cart_items_product");

                // Один SKU в корзине — одна позиция (AddItem увеличивает quantity, не дублирует).
                item.HasIndex(i => new { i.CartId, i.SkuId })
                    .IsUnique()
                    .HasDatabaseName("ux_cart_items_cart_sku");
            });

            builder.Navigation(c => c.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
