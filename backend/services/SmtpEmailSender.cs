using System.Net;
using System.Net.Mail;
using WebApplication1.interfaces;

namespace WebApplication1.services;

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    private string? Host => configuration["Smtp:Host"];
    private string? Username => configuration["Smtp:Username"];
    private string? Password => configuration["Smtp:Password"];
    private string? From => configuration["Smtp:From"];

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password) &&
        MailAddress.TryCreate(From, out _) &&
        int.TryParse(configuration["Smtp:Port"] ?? "587", out var port) &&
        port is > 0 and <= 65535;

    public async Task SendVerificationCodeAsync(
        string email,
        string code,
        CancellationToken cancellationToken,
        bool isPasswordReset = false)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("La configuración SMTP no está completa.");

        using var message = new MailMessage
        {
            From = new MailAddress(From!, configuration["Smtp:DisplayName"] ?? "SERENA"),
            Subject = isPasswordReset ? "Código para restablecer tu contraseña de SERENA" : "Código de verificación de SERENA",
            Body = isPasswordReset
                ? $"""
                <p>Hola,</p>
                <p>Recibimos una solicitud para restablecer la contraseña de tu cuenta SERENA.</p>
                <p>Tu código es:</p>
                <p style="font-size:28px;font-weight:bold;letter-spacing:6px">{WebUtility.HtmlEncode(code)}</p>
                <p>El código vence en 15 minutos. Si no solicitaste este cambio, ignora este mensaje.</p>
                """
                : $"""
                <p>Hola,</p>
                <p>Tu código para verificar el correo en SERENA es:</p>
                <p style="font-size:28px;font-weight:bold;letter-spacing:6px">{WebUtility.HtmlEncode(code)}</p>
                <p>El código vence en 15 minutos. Si no solicitaste esta cuenta, puedes ignorar este mensaje.</p>
                """,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(email));

        using var client = new SmtpClient(Host!, int.TryParse(configuration["Smtp:Port"], out var port) ? port : 587)
        {
            EnableSsl = !bool.TryParse(configuration["Smtp:EnableSsl"], out var enableSsl) || enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(Username, Password),
            Timeout = 15000
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}
