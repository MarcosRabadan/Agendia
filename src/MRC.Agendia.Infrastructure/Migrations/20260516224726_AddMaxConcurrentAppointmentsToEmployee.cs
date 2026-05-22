using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MRC.Agendia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxConcurrentAppointmentsToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing employees default to 1 (one-to-one service), matching the
            // model default. Owners can change this from the API afterwards.
            migrationBuilder.AddColumn<int>(
                name: "MaxConcurrentAppointments",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxConcurrentAppointments",
                table: "Employees");
        }
    }
}
