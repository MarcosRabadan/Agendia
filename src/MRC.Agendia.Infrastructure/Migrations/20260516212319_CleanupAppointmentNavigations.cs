using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MRC.Agendia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanupAppointmentNavigations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Clients_ClientId1",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Employees_EmployeeId1",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Services_ServiceId1",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ClientId1",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_EmployeeId1",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ServiceId1",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ClientId1",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "EmployeeId1",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ServiceId1",
                table: "Appointments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClientId1",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeId1",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceId1",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ClientId1",
                table: "Appointments",
                column: "ClientId1");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_EmployeeId1",
                table: "Appointments",
                column: "EmployeeId1");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ServiceId1",
                table: "Appointments",
                column: "ServiceId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Clients_ClientId1",
                table: "Appointments",
                column: "ClientId1",
                principalTable: "Clients",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Employees_EmployeeId1",
                table: "Appointments",
                column: "EmployeeId1",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Services_ServiceId1",
                table: "Appointments",
                column: "ServiceId1",
                principalTable: "Services",
                principalColumn: "Id");
        }
    }
}
