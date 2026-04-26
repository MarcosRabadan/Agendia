using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MRC.Agendia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRegionFromHolidayCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HolidayCalendar_Date_Scope_Region",
                table: "HolidayCalendars");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "HolidayCalendars");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayCalendar_Date_Scope",
                table: "HolidayCalendars",
                columns: new[] { "Date", "Scope" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HolidayCalendar_Date_Scope",
                table: "HolidayCalendars");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "HolidayCalendars",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HolidayCalendar_Date_Scope_Region",
                table: "HolidayCalendars",
                columns: new[] { "Date", "Scope", "Region" });
        }
    }
}
