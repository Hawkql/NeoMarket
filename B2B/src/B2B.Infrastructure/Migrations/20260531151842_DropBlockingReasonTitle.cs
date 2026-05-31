using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace B2B.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropBlockingReasonTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "blocking_reason_title",
                table: "product");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "blocking_reason_title",
                table: "product",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
