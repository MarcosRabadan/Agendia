using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MRC.Agendia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OneDefaultScheduleTemplatePerBusiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nothing ever stopped a business from having several default templates: IsDefault
            // was written straight from the DTO on create and on update. Any existing database
            // may hold duplicates, and the unique index below would fail to build on them - in
            // Development that means the API refuses to start, since it auto-migrates.
            //
            // So demote the extras first, keeping ONE default per business by the same rule the
            // tie-break uses (latest EffectiveFrom, then Id). This edits data, but the rows it
            // touches are exactly the ones that made the resolution non-deterministic: they were
            // already being ignored at random.
            migrationBuilder.Sql("""
                UPDATE "ScheduleTemplates" t
                SET "IsDefault" = false
                WHERE t."IsDefault"
                  AND t."Id" <> (
                      SELECT k."Id"
                      FROM "ScheduleTemplates" k
                      WHERE k."BusinessId" = t."BusinessId" AND k."IsDefault"
                      ORDER BY k."EffectiveFrom" DESC, k."Id"
                      LIMIT 1
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTemplate_OneDefaultPerBusiness",
                table: "ScheduleTemplates",
                column: "BusinessId",
                unique: true,
                filter: "\"IsDefault\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only the index comes back off. The demoted defaults are NOT restored: which rows
            // were demoted is not recorded anywhere, and re-raising them would recreate exactly
            // the ambiguous state this migration exists to remove.
            migrationBuilder.DropIndex(
                name: "IX_ScheduleTemplate_OneDefaultPerBusiness",
                table: "ScheduleTemplates");
        }
    }
}
