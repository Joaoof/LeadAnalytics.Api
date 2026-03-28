using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadAnalytics.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRelacionamentoLeadUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "leads",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_leads_UnitId",
                table: "leads",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_leads_units_UnitId",
                table: "leads",
                column: "UnitId",
                principalTable: "units",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_leads_units_UnitId",
                table: "leads");

            migrationBuilder.DropIndex(
                name: "IX_leads_UnitId",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "leads");
        }
    }
}
