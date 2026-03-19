using FamilyAccountApi.Features.Email;

namespace FamilyAccountApi.BackgroundJobs;

public sealed class EmailJobs(IEmailService emailService)
{
    public async Task SendPinEmailAsync(string emailTo, string userName, string pin)
    {
        var subject = "Tu PIN de acceso — Family Account";
        var body = $"""
            <html>
            <body style="font-family: Arial, sans-serif; color: #333;">
              <h2>Hola, {userName}</h2>
              <p>Tu PIN de acceso a <strong>Family Account</strong> es:</p>
              <div style="background:#f0f0f0;padding:16px;border-radius:8px;text-align:center;font-size:32px;letter-spacing:8px;font-weight:bold;">
                {pin}
              </div>
              <p style="margin-top:16px;">Este PIN es de un solo uso. Una vez que inicies sesión, será eliminado.</p>
              <p style="color:#888;font-size:12px;">Si no solicitaste este PIN, ignora este correo.</p>
            </body>
            </html>
            """;

        await emailService.SendAsync(emailTo, subject, body, isHtml: true);
    }
}
