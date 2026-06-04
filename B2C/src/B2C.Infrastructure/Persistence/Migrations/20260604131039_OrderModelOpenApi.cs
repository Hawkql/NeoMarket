using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace B2C.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrderModelOpenApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_total_non_negative",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "delivery_address",
                table: "orders");

            migrationBuilder.RenameColumn(
                name: "total_amount",
                table: "orders",
                newName: "subtotal");

            migrationBuilder.AddColumn<string>(
                name: "address_apartment",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_city",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "address_country",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "address_house",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "address_original_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_postal_code",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_street",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "cancel_reason",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "comment",
                table: "orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "delivered_at",
                table: "orders",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "delivery_cost",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "paid_at",
                table: "orders",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "payment_method_id",
                table: "orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "payment_method_type",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "CARD");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_subtotal_non_negative",
                table: "orders",
                sql: "subtotal >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_subtotal_non_negative",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "address_apartment",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "address_city",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "address_country",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "address_house",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "address_original_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "address_postal_code",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "address_street",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "cancel_reason",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "comment",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "delivered_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "delivery_cost",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "paid_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "payment_method_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "payment_method_type",
                table: "orders");

            migrationBuilder.RenameColumn(
                name: "subtotal",
                table: "orders",
                newName: "total_amount");

            migrationBuilder.AddColumn<string>(
                name: "delivery_address",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_total_non_negative",
                table: "orders",
                sql: "total_amount >= 0");
        }
    }
}
