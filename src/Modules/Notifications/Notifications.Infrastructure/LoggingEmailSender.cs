using Microsoft.Extensions.Logging;
using Notifications.Application;

namespace Notifications.Infrastructure;

/// <summary>
/// Used when no SMTP host is configured — logs the notification instead of sending it, so
/// local dev and this sandboxed environment can exercise the whole notification pipeline
/// (including idempotency via NotificationLog) without a real mail server.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
    {
        _logger.LogInformation("Email (no SMTP configured, logged only) to {ToEmail}: {Subject} — {Body}", toEmail, subject, body);
        return Task.CompletedTask;
    }
}
