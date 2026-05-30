using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MRC.Agendia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitlistBusinessFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_WaitlistEntries_Businesses_BusinessId",
                table: "WaitlistEntries",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WaitlistEntries_Businesses_BusinessId",
                table: "WaitlistEntries");
        }
    }
}
