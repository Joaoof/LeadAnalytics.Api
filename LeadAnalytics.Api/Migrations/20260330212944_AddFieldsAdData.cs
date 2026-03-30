using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadAnalytics.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldsAdData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ad",
                table: "leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Campaign",
                table: "leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceFinal",
                table: "leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingConfidence",
                table: "leads",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ad",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "Campaign",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "SourceFinal",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "TrackingConfidence",
                table: "leads");
        }
    }
}
