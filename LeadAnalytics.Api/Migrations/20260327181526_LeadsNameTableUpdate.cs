using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace LeadAnalytics.Api.Migrations
{
    /// <inheritdoc />
    public partial class LeadsNameTableUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_leads_ExternalId_TenantId",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "AdData",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "ConvertedAt",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "CustomFields",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "HasAppointment",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "leads");

            migrationBuilder.CreateTable(
                name: "TagDto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagDto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TagDto_leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "leads",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_leads_Id_TenantId",
                table: "leads",
                columns: new[] { "Id", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TagDto_LeadId",
                table: "TagDto",
                column: "LeadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TagDto");

            migrationBuilder.DropIndex(
                name: "IX_leads_Id_TenantId",
                table: "leads");

            migrationBuilder.AddColumn<string>(
                name: "AdData",
                table: "leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConvertedAt",
                table: "leads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomFields",
                table: "leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAppointment",
                table: "leads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "leads",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "leads",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_leads_ExternalId_TenantId",
                table: "leads",
                columns: new[] { "ExternalId", "TenantId" },
                unique: true);
        }
    }
}
