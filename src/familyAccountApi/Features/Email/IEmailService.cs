namespace FamilyAccountApi.Features.Email;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, bool isHtml = false, CancellationToken ct = default);
}
