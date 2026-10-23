using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandilyCommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailSendLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailSendLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToAddress = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AttemptedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSendLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendLogs_AttemptedAtUtc",
                table: "EmailSendLogs",
                column: "AttemptedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendLogs_LeadId",
                table: "EmailSendLogs",
                column: "LeadId");
        
            // Supabase Security Advisor: RLS on, no policies (API connects as owner; anon/PostgREST denied).
            migrationBuilder.Sql("ALTER TABLE public.\"EmailSendLogs\" ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailSendLogs");
        }
    }
}
