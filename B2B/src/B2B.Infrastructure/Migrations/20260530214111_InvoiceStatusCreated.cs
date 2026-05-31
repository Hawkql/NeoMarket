using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace B2B.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InvoiceStatusCreated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_invoices_status_pending",
                table: "invoices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_invoices_accepted_at_consistency",
                table: "invoices");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_status_Created",
                table: "invoices",
                column: "status",
                filter: "status = 'Created'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_invoices_accepted_at_consistency",
                table: "invoices",
                sql: "(status = 'Created' AND accepted_at IS NULL) OR (status <> 'Created')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_invoices_status_Created",
                table: "invoices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_invoices_accepted_at_consistency",
                table: "invoices");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_status_pending",
                table: "invoices",
                column: "status",
                filter: "status = 'Pending'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_invoices_accepted_at_consistency",
                table: "invoices",
                sql: "(status = 'Pending' AND accepted_at IS NULL) OR (status <> 'Pending')");
        }
    }
}
