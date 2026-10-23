using System.Globalization;
using System.Net;
using HandilyCommerce.Domain.Leads;

namespace HandilyCommerce.Application.Leads;

/// <summary>Responsive HTML (table layout, inline CSS) for the "new lead" e-mail. All values are HTML-encoded.</summary>
public static class LeadEmailTemplate
{
    private static readonly TimeZoneInfo BrazilTime = ResolveBrazilTime();

    public static string Build(Lead lead)
    {
        ArgumentNullException.ThrowIfNull(lead);

        var createdAt = TimeZoneInfo.ConvertTime(lead.CreatedAt, BrazilTime)
            .ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

        var rows = string.Concat(
            Row("Nome", lead.Name),
            Row("E-mail", lead.Email),
            Row("Município", lead.Municipality),
            Row("Mensagem", lead.Message ?? "—"),
            Row("Recebido em", $"{createdAt} (horário de Brasília)"));

        return $$"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Novo contato</title>
            </head>
            <body style="margin:0;padding:0;background-color:#f1f5f9;font-family:Arial,Helvetica,sans-serif;">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:#f1f5f9;padding:24px 12px;">
            <tr><td align="center">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:600px;background-color:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(15,23,42,0.08);">
            <tr><td style="background-color:#1e3a8a;padding:24px;color:#ffffff;">
            <p style="margin:0;font-size:12px;letter-spacing:1px;text-transform:uppercase;opacity:0.85;">Handily Commerce · Lead BCC</p>
            <h1 style="margin:8px 0 0;font-size:22px;line-height:1.3;">Novo contato recebido</h1>
            </td></tr>
            <tr><td style="padding:24px;">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="border-collapse:collapse;">
            {{rows}}
            </table>
            </td></tr>
            <tr><td style="padding:16px 24px;background-color:#f8fafc;color:#64748b;font-size:12px;text-align:center;">
            Mensagem automática enviada pelo formulário "Fale conosco".
            </td></tr>
            </table>
            </td></tr>
            </table>
            </body>
            </html>
            """;
    }

    private static string Row(string label, string value) =>
        "<tr>"
        + "<td style=\"padding:10px 0;border-bottom:1px solid #e2e8f0;color:#64748b;font-size:13px;width:35%;vertical-align:top;\">"
        + WebUtility.HtmlEncode(label) + "</td>"
        + "<td style=\"padding:10px 0;border-bottom:1px solid #e2e8f0;color:#0f172a;font-size:15px;font-weight:bold;word-break:break-word;\">"
        + WebUtility.HtmlEncode(value) + "</td>"
        + "</tr>";

    private static TimeZoneInfo ResolveBrazilTime()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "BRT", "BRT");
        }
    }
}
