using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MRC.Agendia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentStartDateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Appointment_StartDate",
                table: "Appointments",
                column: "StartDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointment_StartDate",
                table: "Appointments");
        }
    }
}
