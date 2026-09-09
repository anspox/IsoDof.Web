namespace IsoDof.Web.Services;

public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string toEmail, string subject, string body)
    {
        _logger.LogInformation("--- MAIL (gerçekte gönderilmedi) ---\nKime: {ToEmail}\nKonu: {Subject}\nİçerik:\n{Body}\n------------------------------------",
            toEmail, subject, body);
        return Task.CompletedTask;
    }
}