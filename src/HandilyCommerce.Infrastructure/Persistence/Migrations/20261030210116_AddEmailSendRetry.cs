using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandilyCommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailSendRetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "EmailSendLogs",
                type: "integer",
                nullable: false,
                // Backfill: every existing row was exactly one (final) attempt.
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAtUtc",
                table: "EmailSendLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendLogs_Status_NextAttemptAtUtc",
                table: "EmailSendLogs",
                columns: new[] { "Status", "NextAttemptAtUtc" });

            // Existing rows are final (Sent/Failed/Skipped): NextAttemptAtUtc stays NULL, so nothing is re-sent.
            // RLS on EmailSendLogs (AddEmailSendLogs) is unaffected by ADD COLUMN; re-assert idempotently.
            migrationBuilder.Sql("ALTER TABLE public.\"EmailSendLogs\" ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailSendLogs_Status_NextAttemptAtUtc",
                table: "EmailSendLogs");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "EmailSendLogs");

            migrationBuilder.DropColumn(
                name: "NextAttemptAtUtc",
                table: "EmailSendLogs");
        }
    }
}
