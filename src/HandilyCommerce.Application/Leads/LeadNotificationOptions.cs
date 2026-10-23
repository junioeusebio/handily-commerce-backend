namespace HandilyCommerce.Application.Leads;

/// <summary>Config section <c>LeadNotification</c>.</summary>
public sealed class LeadNotificationOptions
{
    public const string SectionName = "LeadNotification";
    public const string DefaultTo = "ajksys@protonmail.com";
    public const string DefaultSubject = "[NOVO CONTATO] - Lead BCC";

    public string To { get; set; } = DefaultTo;

    public string Subject { get; set; } = DefaultSubject;
}
