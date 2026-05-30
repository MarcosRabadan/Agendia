using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MRC.Agendia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitlistUniqueWaitingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntry_UniqueWaiting",
                table: "WaitlistEntries",
                columns: new[] { "BusinessId", "ServiceId", "Date", "StartTime", "EmployeeId", "ClientId" },
                unique: true,
                filter: "[Status] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WaitlistEntry_UniqueWaiting",
                table: "WaitlistEntries");
        }
    }
}
