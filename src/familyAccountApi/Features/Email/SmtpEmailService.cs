using System.Net;
using System.Net.Mail;
using FamilyAccountApi.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FamilyAccountApi.Features.Email;

public sealed class SmtpEmailService(IOptions<SmtpOptions> smtpOptions) : IEmailService
{
    private readonly SmtpOptions _smtp = smtpOptions.Value;

    public async Task SendAsync(
        string to, string subject, string body,
        bool isHtml = false, CancellationToken ct = default)
    {
        using var client = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            Credentials = new NetworkCredential(_smtp.Username, _smtp.Password),
            EnableSsl = _smtp.EnableSsl
        };

        var from = new MailAddress(_smtp.FromEmail, _smtp.FromName);
        using var message = new MailMessage(from, new MailAddress(to))
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };

        await client.SendMailAsync(message, ct);
    }
}
