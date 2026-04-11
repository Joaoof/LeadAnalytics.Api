using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeadAnalytics.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDbSetAppDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WebhookEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    PhoneNumberId = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<int>(type: "integer", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OriginEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContactName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CtwaClid = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Headline = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MessageTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Processed = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: true),
                    WebhookEventId = table.Column<int>(type: "integer", nullable: true),
                    Confidence = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OriginEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OriginEvents_WebhookEvents_WebhookEventId",
                        column: x => x.WebhookEventId,
                        principalTable: "WebhookEvents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LeadAttributions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeadId = table.Column<int>(type: "integer", nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CtwaClid = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MatchType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Confidence = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OriginEventId = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadAttributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadAttributions_OriginEvents_OriginEventId",
                        column: x => x.OriginEventId,
                        principalTable: "OriginEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeadAttributions_OriginEventId",
                table: "LeadAttributions",
                column: "OriginEventId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginEvents_WebhookEventId",
                table: "OriginEvents",
                column: "WebhookEventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeadAttributions");

            migrationBuilder.DropTable(
                name: "OriginEvents");

            migrationBuilder.DropTable(
                name: "WebhookEvents");
        }
    }
}
