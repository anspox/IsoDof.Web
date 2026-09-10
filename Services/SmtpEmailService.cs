using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace IsoDof.Web.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpSection = _configuration.GetSection("Smtp");
        var host = smtpSection["Host"];
        var port = int.Parse(smtpSection["Port"] ?? "587");
        var fromEmail = smtpSection["FromEmail"];
        var fromName = smtpSection["FromName"];
        var username = smtpSection["Username"];
        var password = smtpSection["Password"];

        // Eğer SMTP şifresi veya sunucu ayarı yapılmamışsa Gmail'e bağlanıp 30 saniye bekletme!
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("SMTP şifresi yapılandırılmadığı için e-posta gönderimi simüle edildi: {ToEmail} - {Subject}", toEmail, subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName ?? "ISO DÖF", fromEmail ?? "noreply@isodof.com"));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        client.Timeout = 3000; // En fazla 3 saniye bekle
        try
        {
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Mail gönderildi: {ToEmail} - {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Mail gönderilemedi (zaman aşımı veya kimlik doğrulama): {Message}", ex.Message);
        }
    }
}