using HandilyCommerce.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HandilyCommerce.Infrastructure.Notifications;

/// <summary>EF mapping for <see cref="EmailSendLog"/> → <c>EmailSendLogs</c>.</summary>
public static class EmailSendLogModel
{
    public const string TableName = "EmailSendLogs";

    public static void Configure(EntityTypeBuilder<EmailSendLog> entity)
    {
        entity.ToTable(TableName);
        entity.HasKey(e => e.Id);
        entity.Property(e => e.ToAddress).IsRequired().HasMaxLength(254);
        entity.Property(e => e.Subject).IsRequired().HasMaxLength(200);
        entity.Property(e => e.Provider).IsRequired().HasMaxLength(32);
        entity.Property(e => e.ProviderMessageId).HasMaxLength(128);
        entity.Property(e => e.Status).IsRequired().HasConversion<string>().HasMaxLength(16);
        entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
        entity.Property(e => e.AttemptedAtUtc).IsRequired();
        entity.Property(e => e.AttemptCount).IsRequired();
        entity.Property(e => e.NextAttemptAtUtc);
        entity.Ignore(e => e.IsOpen);
        entity.HasIndex(e => e.LeadId);
        entity.HasIndex(e => new { e.Status, e.NextAttemptAtUtc });
        entity.HasIndex(e => e.AttemptedAtUtc);
    }
}
