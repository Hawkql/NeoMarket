using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace B2B.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ordering = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                    table.CheckConstraint("ck_categories_no_self_parent", "parent_id IS NULL OR parent_id <> id");
                    table.CheckConstraint("ck_categories_ordering_non_negative", "ordering >= 0");
                    table.ForeignKey(
                        name: "FK_categories_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordering = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_images", x => x.id);
                    table.CheckConstraint("ck_images_entity_type_valid", "entity_type IN ('product', 'sku')");
                    table.CheckConstraint("ck_images_ordering_non_negative", "ordering >= 0");
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                columns: table => new
                {
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false),
                    message_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    received_on_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    processed_on_utc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => x.idempotency_key);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    accepted_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.Id);
                    table.CheckConstraint("ck_invoices_accepted_at_consistency", "(status = 'Pending' AND accepted_at IS NULL) OR (status <> 'Pending' AND accepted_at IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    destination = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    aggregate_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    occurred_on_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    processed_on_utc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    blocking_reason_reason_id = table.Column<Guid>(type: "uuid", nullable: true),
                    blocking_reason_title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    blocking_reason_comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    moderation_round = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product", x => x.id);
                    table.CheckConstraint("ck_products_moderation_round_non_negative", "moderation_round >= 0");
                });

            migrationBuilder.CreateTable(
                name: "skus",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    price = table.Column<int>(type: "integer", nullable: false),
                    cost_price = table.Column<int>(type: "integer", nullable: false),
                    discount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    image = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    active_quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    reserved_quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skus", x => x.id);
                    table.CheckConstraint("ck_skus_active_quantity_non_negative", "active_quantity >= 0");
                    table.CheckConstraint("ck_skus_cost_price_positive", "cost_price > 0");
                    table.CheckConstraint("ck_skus_discount_valid", "discount >= 0 AND discount < price");
                    table.CheckConstraint("ck_skus_price_positive", "price > 0");
                    table.CheckConstraint("ck_skus_reserved_quantity_non_negative", "reserved_quantity >= 0");
                });

            migrationBuilder.CreateTable(
                name: "invoice_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    accepted_quantity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_items", x => x.id);
                    table.CheckConstraint("ck_invoice_items_accepted_quantity_valid", "accepted_quantity IS NULL OR (accepted_quantity >= 0 AND accepted_quantity <= quantity)");
                    table.CheckConstraint("ck_invoice_items_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "FK_invoice_items_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "field_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sku_id = table.Column<Guid>(type: "uuid", nullable: true),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    moderation_round = table.Column<int>(type: "integer", nullable: false),
                    reported_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_field_reports", x => x.id);
                    table.CheckConstraint("ck_field_reports_round_positive", "moderation_round > 0");
                    table.ForeignKey(
                        name: "FK_field_reports_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_characteristics",
                columns: table => new
                {
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_characteristics", x => new { x.product_id, x.id });
                    table.ForeignKey(
                        name: "FK_product_characteristics_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sku_characteristics",
                columns: table => new
                {
                    sku_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sku_characteristics", x => new { x.sku_id, x.id });
                    table.ForeignKey(
                        name: "FK_sku_characteristics_skus_sku_id",
                        column: x => x.sku_id,
                        principalTable: "skus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_active",
                table: "categories",
                column: "parent_id",
                filter: "deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_ordering",
                table: "categories",
                columns: new[] { "parent_id", "ordering" });

            migrationBuilder.CreateIndex(
                name: "ix_field_reports_product",
                table: "field_reports",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_field_reports_sku",
                table: "field_reports",
                column: "sku_id");

            migrationBuilder.CreateIndex(
                name: "ix_images_owner",
                table: "images",
                columns: new[] { "entity_type", "entity_id", "ordering" });

            migrationBuilder.CreateIndex(
                name: "ix_inbox_unprocessed",
                table: "inbox_messages",
                column: "received_on_utc",
                filter: "processed_on_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_items_sku",
                table: "invoice_items",
                column: "sku_id");

            migrationBuilder.CreateIndex(
                name: "ux_invoice_items_invoice_sku",
                table: "invoice_items",
                columns: new[] { "invoice_id", "sku_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_seller_status_created",
                table: "invoices",
                columns: new[] { "seller_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_status_pending",
                table: "invoices",
                column: "status",
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_aggregate",
                table: "outbox_messages",
                columns: new[] { "aggregate_type", "aggregate_id" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_unprocessed",
                table: "outbox_messages",
                column: "occurred_on_utc",
                filter: "processed_on_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_products_category",
                table: "product",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_seller_status",
                table: "product",
                columns: new[] { "seller_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_products_status_deleted",
                table: "product",
                columns: new[] { "status", "deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_skus_product_active",
                table: "skus",
                column: "product_id",
                filter: "deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "field_reports");

            migrationBuilder.DropTable(
                name: "images");

            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "invoice_items");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "product_characteristics");

            migrationBuilder.DropTable(
                name: "sku_characteristics");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "product");

            migrationBuilder.DropTable(
                name: "skus");
        }
    }
}
