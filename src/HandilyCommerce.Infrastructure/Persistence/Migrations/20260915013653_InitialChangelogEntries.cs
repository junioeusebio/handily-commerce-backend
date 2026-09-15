using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HandilyCommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialChangelogEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChangelogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ProductVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MergedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PrNumber = table.Column<int>(type: "integer", nullable: true),
                    Label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangelogEntries", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ChangelogEntries",
                columns: new[] { "Id", "Label", "MergedAt", "PrNumber", "ProductVersion", "Summary", "Title" },
                values: new object[,]
                {
                    { new Guid("a10c0000-0007-4000-8000-000000000007"), "Release Mirror", new DateTimeOffset(new DateTime(2026, 9, 9, 0, 41, 37, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 7, "0.1.0", "Introduces product Versioning separate from the API route version, with Directory.Build.props baseline 0.1.0 and the Release label process.", "chore: Versioning 0.1.0 + Release labels process" },
                    { new Guid("a10c0000-0010-4000-8000-00000000000a"), "Release Mirror", new DateTimeOffset(new DateTime(2026, 9, 9, 14, 39, 41, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 10, "0.2.0", "Adds Scalar UI over the existing OpenAPI document in all environments, including Production on Render.", "feat(A2): Scalar OpenAPI UI" },
                    { new Guid("a10c0000-0011-4000-8000-00000000000b"), "Release Mirror", new DateTimeOffset(new DateTime(2026, 9, 10, 22, 44, 17, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 11, "0.3.0", "Changelog API for the FE \"What's new\" modal: hexagonal ports, GET /api/v1/changelog from embedded JSON seed, and merge workflow to append Major/Mirror entries.", "feat: changelog API for Major/Mirror releases" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangelogEntries");
        }
    }
}
