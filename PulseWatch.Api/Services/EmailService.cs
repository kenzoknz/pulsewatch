using System.Net;
using System.Net.Mail;

namespace PulseWatch.Api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpServer = _config["Smtp:Server"];
        var smtpPortStr = _config["Smtp:Port"];
        var smtpUser = _config["Smtp:Username"];
        var smtpPass = _config["Smtp:Password"];
        var fromEmail = _config["Smtp:FromAddress"] ?? "noreply@pulsewatch.com";

        if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpUser))
        {
            _logger.LogWarning("[MOCK EMAIL] To: {ToEmail} | Subject: {Subject} | Body: {Body}", toEmail, subject, body);
            return;
        }

        try
        {
            int smtpPort = int.TryParse(smtpPortStr, out var p) ? p : 587;
            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, "PulseWatch Alerts"),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Alert email sent to {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alert email to {ToEmail}", toEmail);
        }
    }
}
