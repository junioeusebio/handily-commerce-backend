using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandilyCommerce.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Admin fields on <c>Leads</c>: Status (existing rows become <c>Novo</c> via the column default) and InternalNote.
    /// RLS: <c>Leads</c> already has RLS enabled (EnableRowLevelSecurity, #31) and it is table-wide, so the new
    /// columns are covered without extra SQL; no new table is created.
    /// </summary>
    public partial class AddLeadAdminFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InternalNote",
                table: "Leads",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Leads",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Novo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InternalNote",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Leads");
        }
    }
}
