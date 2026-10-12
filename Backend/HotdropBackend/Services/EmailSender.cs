using System.Text;
using System.Net.Mail;

namespace HotdropBackend.Services;

public sealed record EmailMessage(string To, string Subject, string TextBody);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

/// <summary>
/// "Email" configuration. With no Smtp:Host the backend writes every outgoing
/// message to a local outbox directory (zero cost, no external service); an
/// operator can relay them by hand or point Smtp at a real server later.
/// </summary>
public sealed class EmailOptions
{
    public string From { get; set; } = "no-reply@updraft.invalid";
    /// <summary>Defaults to &lt;app dir&gt;/outbox.</summary>
    public string? OutboxDirectory { get; set; }
    public SmtpSettings Smtp { get; set; } = new();

    public sealed class SmtpSettings
    {
        public string? Host { get; set; }
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public bool SmtpConfigured => !string.IsNullOrWhiteSpace(Smtp.Host);
    public string ResolvedOutboxDirectory => string.IsNullOrWhiteSpace(OutboxDirectory)
        ? Path.Combine(AppContext.BaseDirectory, "outbox")
        : OutboxDirectory;

    public void Validate(Microsoft.Extensions.Hosting.IHostEnvironment environment)
    {
        if (!environment.IsProduction()) return;

        Smtp.Host = Smtp.Host?.Trim();
        From = From?.Trim() ?? string.Empty;
        if (!SmtpConfigured)
            throw new InvalidOperationException("Production requires Email:Smtp:Host; the local email outbox is not delivery.");
        if (Smtp.Port is < 1 or > 65535)
            throw new InvalidOperationException("Email:Smtp:Port must be between 1 and 65535.");
        if (!Smtp.EnableSsl)
            throw new InvalidOperationException("Production email delivery requires Email:Smtp:EnableSsl=true.");
        if (!MailAddress.TryCreate(From, out var from) || from.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production requires a valid, publisher-controlled Email:From address.");

        bool hasUsername = !string.IsNullOrWhiteSpace(Smtp.Username);
        bool hasPassword = !string.IsNullOrWhiteSpace(Smtp.Password);
        if (hasUsername != hasPassword)
            throw new InvalidOperationException("Email:Smtp:Username and Email:Smtp:Password must be configured together.");
    }
}

/// <summary>Default sender: one .eml-style text file per message in the outbox directory.</summary>
public sealed class OutboxEmailSender(EmailOptions options, ILogger<OutboxEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        string dir = options.ResolvedOutboxDirectory;
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.eml");
        var content = new StringBuilder()
            .Append("From: ").Append(options.From).Append("\r\n")
            .Append("To: ").Append(message.To).Append("\r\n")
            .Append("Subject: ").Append(message.Subject).Append("\r\n")
            .Append("Date: ").Append(DateTimeOffset.UtcNow.ToString("r")).Append("\r\n")
            .Append("Content-Type: text/plain; charset=utf-8\r\n\r\n")
            .Append(message.TextBody)
            .ToString();
        await File.WriteAllTextAsync(path, content, Encoding.UTF8, cancellationToken);
        logger.LogInformation("Email queued to local outbox {Path}", path);
    }
}

/// <summary>Real delivery once Email:Smtp:Host (and credentials) are configured.</summary>
public sealed class SmtpEmailSender(EmailOptions options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        using var client = new System.Net.Mail.SmtpClient(options.Smtp.Host, options.Smtp.Port)
        {
            EnableSsl = options.Smtp.EnableSsl,
        };
        if (!string.IsNullOrEmpty(options.Smtp.Username))
            client.Credentials = new System.Net.NetworkCredential(options.Smtp.Username, options.Smtp.Password);
        using var mail = new System.Net.Mail.MailMessage(options.From, message.To, message.Subject, message.TextBody);
        await client.SendMailAsync(mail, cancellationToken);
    }
}

