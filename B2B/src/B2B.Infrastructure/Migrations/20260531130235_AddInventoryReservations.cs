using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace B2B.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_reservations", x => x.id);
                    table.CheckConstraint("ck_inventory_reservations_quantity_positive", "quantity > 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_reservations_order",
                table: "inventory_reservations",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ux_inventory_reservations_order_sku",
                table: "inventory_reservations",
                columns: new[] { "order_id", "sku_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_reservations");
        }
    }
}
