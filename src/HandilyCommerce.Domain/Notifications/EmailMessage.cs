namespace HandilyCommerce.Domain.Notifications;

/// <summary>Outgoing HTML e-mail.</summary>
public sealed record EmailMessage(string To, string Subject, string HtmlBody);
