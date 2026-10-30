using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HandilyCommerce.Domain.Notifications;

namespace HandilyCommerce.Infrastructure.Notifications;

/// <summary>
/// <see cref="IEmailSender"/> adapter for Resend (POST /emails, Bearer API key). Typed HttpClient.
/// Missing API key → <see cref="EmailSendStatus.Skipped"/> (no HTTP call).
/// </summary>
public sealed class ResendEmailSender(HttpClient httpClient, ResendOptions options) : IEmailSender
{
    public const string ProviderName = "Resend";
    private const int MaxBodyInError = 500;

    public string Provider => ProviderName;

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return EmailSendResult.Skipped("Resend API key not configured (RESEND__APIKEY).");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(options.BaseUrl), "emails"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = JsonContent.Create(new ResendRequest(options.From, [message.To], message.Subject, message.HtmlBody));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var snippet = body.Length > MaxBodyInError ? body[..MaxBodyInError] : body;
            var code = (int)response.StatusCode;
            // 429 and 5xx are transient; other 4xx (bad request, invalid key, …) are final.
            return EmailSendResult.Failed($"HTTP {code}: {snippet}", retryable: code == 429 || code >= 500);
        }

        return EmailSendResult.Sent(TryReadId(body));
    }

    private static string? TryReadId(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<ResendResponse>(body)?.Id;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record ResendRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string[] To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html);

    private sealed record ResendResponse([property: JsonPropertyName("id")] string? Id);
}
