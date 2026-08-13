using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MRC.Agendia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveClientEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Clients_ClientId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitlistEntries_Clients_ClientId",
                table: "WaitlistEntries");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_WaitlistEntries_ClientId",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_WaitlistEntry_UniqueWaiting",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ClientId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "WaitlistEntries");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Appointments");

            migrationBuilder.AddColumn<string>(
                name: "ClientUserId",
                table: "WaitlistEntries",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientUserId",
                table: "Appointments",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntry_ClientUserId",
                table: "WaitlistEntries",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntry_UniqueWaiting",
                table: "WaitlistEntries",
                columns: new[] { "BusinessId", "ServiceId", "Date", "StartTime", "EmployeeId", "ClientUserId" },
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_ClientUserId",
                table: "Appointments",
                column: "ClientUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WaitlistEntry_ClientUserId",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_WaitlistEntry_UniqueWaiting",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_Appointment_ClientUserId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ClientUserId",
                table: "WaitlistEntries");

            migrationBuilder.DropColumn(
                name: "ClientUserId",
                table: "Appointments");

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "WaitlistEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "Appointments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clients_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_ClientId",
                table: "WaitlistEntries",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntry_UniqueWaiting",
                table: "WaitlistEntries",
                columns: new[] { "BusinessId", "ServiceId", "Date", "StartTime", "EmployeeId", "ClientId" },
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ClientId",
                table: "Appointments",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Client_BusinessId",
                table: "Clients",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Client_UserId",
                table: "Clients",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_IsDeleted",
                table: "Clients",
                column: "IsDeleted");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Clients_ClientId",
                table: "Appointments",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WaitlistEntries_Clients_ClientId",
                table: "WaitlistEntries",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
