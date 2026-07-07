using System.Net;
using System.Net.Mail;
using Notifications.Application;

namespace Notifications.Infrastructure;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;

    public SmtpEmailSender(SmtpOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = string.IsNullOrWhiteSpace(_options.Username)
                ? null
                : new NetworkCredential(_options.Username, _options.Password)
        };

        using var message = new MailMessage(_options.FromAddress, toEmail, subject, body);
        await client.SendMailAsync(message, ct);
    }
}

public sealed class SmtpOptions
{
    public const string SectionName = "Notifications:Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "no-reply@ticket-triage.local";
}
