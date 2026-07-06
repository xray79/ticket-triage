using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Triage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueSucceededTriageRecordIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_triage_records_TicketId",
                schema: "triage",
                table: "triage_records");

            migrationBuilder.CreateIndex(
                name: "IX_triage_records_TicketId",
                schema: "triage",
                table: "triage_records",
                column: "TicketId",
                unique: true,
                filter: "\"Succeeded\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_triage_records_TicketId",
                schema: "triage",
                table: "triage_records");

            migrationBuilder.CreateIndex(
                name: "IX_triage_records_TicketId",
                schema: "triage",
                table: "triage_records",
                column: "TicketId");
        }
    }
}
