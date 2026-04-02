using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeadAnalytics.Api.Migrations
{
    /// <inheritdoc />
    public partial class AlterTableLeads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Origin",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "SourceFinal",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "leads");

            migrationBuilder.RenameColumn(
                name: "Stage",
                table: "LeadStageHistory",
                newName: "StageLabel");

            migrationBuilder.RenameColumn(
                name: "IdStage",
                table: "leads",
                newName: "CurrentStageId");

            migrationBuilder.AlterColumn<int>(
                name: "StageId",
                table: "LeadStageHistory",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TrackingConfidence",
                table: "leads",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Campaign",
                table: "leads",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "leads",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "leads",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "LeadConversation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeadId = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: true),
                    ConversationState = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadConversation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadConversation_leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeadInteraction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeadConversationId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadInteraction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadInteraction_LeadConversation_LeadConversationId",
                        column: x => x.LeadConversationId,
                        principalTable: "LeadConversation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeadConversation_LeadId",
                table: "LeadConversation",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadInteraction_LeadConversationId",
                table: "LeadInteraction",
                column: "LeadConversationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeadInteraction");

            migrationBuilder.DropTable(
                name: "LeadConversation");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "leads");

            migrationBuilder.RenameColumn(
                name: "StageLabel",
                table: "LeadStageHistory",
                newName: "Stage");

            migrationBuilder.RenameColumn(
                name: "CurrentStageId",
                table: "leads",
                newName: "IdStage");

            migrationBuilder.AlterColumn<int>(
                name: "StageId",
                table: "LeadStageHistory",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "TrackingConfidence",
                table: "leads",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Campaign",
                table: "leads",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceFinal",
                table: "leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stage",
                table: "leads",
                type: "text",
                nullable: true);
        }
    }
}
