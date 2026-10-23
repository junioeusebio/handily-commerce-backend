namespace HandilyCommerce.Infrastructure.Notifications;

/// <summary>Config section <c>Resend</c>. ApiKey comes from env <c>RESEND__APIKEY</c> (never committed or logged).</summary>
public sealed class ResendOptions
{
    public const string SectionName = "Resend";
    public const string DefaultFrom = "Handily Commerce <onboarding@resend.dev>";
    public const string DefaultBaseUrl = "https://api.resend.com/";

    public string? ApiKey { get; set; }

    public string From { get; set; } = DefaultFrom;

    public string BaseUrl { get; set; } = DefaultBaseUrl;
}
