using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MRC.Agendia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceBusinessScheduleWithScheduleSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessSchedules");

            migrationBuilder.CreateTable(
                name: "HolidayCalendars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolidayCalendars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    OverrideType = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleOverrides_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleTemplates_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomTimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleOverrideId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomTimeSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomTimeSlots_ScheduleOverrides_ScheduleOverrideId",
                        column: x => x.ScheduleOverrideId,
                        principalTable: "ScheduleOverrides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyTimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleTemplateId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    SlotType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyTimeSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyTimeSlots_ScheduleTemplates_ScheduleTemplateId",
                        column: x => x.ScheduleTemplateId,
                        principalTable: "ScheduleTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomTimeSlots_ScheduleOverrideId",
                table: "CustomTimeSlots",
                column: "ScheduleOverrideId");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayCalendar_Date_Scope_Region",
                table: "HolidayCalendars",
                columns: new[] { "Date", "Scope", "Region" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleOverride_BusinessId_Date",
                table: "ScheduleOverrides",
                columns: new[] { "BusinessId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTemplates_BusinessId",
                table: "ScheduleTemplates",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTimeSlots_ScheduleTemplateId",
                table: "WeeklyTimeSlots",
                column: "ScheduleTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomTimeSlots");

            migrationBuilder.DropTable(
                name: "HolidayCalendars");

            migrationBuilder.DropTable(
                name: "WeeklyTimeSlots");

            migrationBuilder.DropTable(
                name: "ScheduleOverrides");

            migrationBuilder.DropTable(
                name: "ScheduleTemplates");

            migrationBuilder.CreateTable(
                name: "BusinessSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsWorkingDay = table.Column<bool>(type: "bit", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessSchedules_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSchedules_BusinessId",
                table: "BusinessSchedules",
                column: "BusinessId");
        }
    }
}
