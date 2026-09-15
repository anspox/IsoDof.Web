using IsoDof.Web.Data;
using IsoDof.Web.Models.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace IsoDof.Web.Services;

/// <summary>
/// Periyodik olarak çalışıp iki şeyi kontrol eder:
///  1) Termine 3 gün veya 1 gün kalan açık DÖF'ler için atanan kişiye SLA uyarısı gönderir.
///  2) Kapatıldıktan sonra etkinlik kontrolü tarihi gelmiş (ve henüz değerlendirilmemiş) DÖF'ler için
///     kalite sorumlusuna/atanan kişiye "alınan önlem kalıcı oldu mu?" hatırlatması gönderir.
/// </summary>
public class DofReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);
    private static readonly int[] SlaThresholdDays = { 3, 1 };

    private readonly IServiceProvider _services;
    private readonly ILogger<DofReminderBackgroundService> _logger;

    public DofReminderBackgroundService(IServiceProvider services, ILogger<DofReminderBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DÖF hatırlatma servisinde beklenmeyen hata oluştu.");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Uygulama kapanıyor
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        await SendSlaRemindersAsync(context, notifications, emailService, stoppingToken);
        await SendEffectivenessRemindersAsync(context, notifications, stoppingToken);
    }

    private async Task SendSlaRemindersAsync(AppDbContext context, INotificationService notifications,
        IEmailService emailService, CancellationToken stoppingToken)
    {
        var now = DateTime.UtcNow;

        var candidates = await context.Dofs
            .Include(d => d.AssignedToUser)
            .Where(d => !d.IsArchived
                && d.Status != DofStatus.Kapatildi && d.Status != DofStatus.Reddedildi
                && d.DueDate.HasValue && d.DueDate.Value > now
                && d.AssignedToUserId.HasValue)
            .ToListAsync(stoppingToken);

        foreach (var dof in candidates)
        {
            var daysRemaining = (int)Math.Ceiling((dof.DueDate!.Value - now).TotalDays);

            foreach (var threshold in SlaThresholdDays)
            {
                if (daysRemaining > threshold) continue;
                if (dof.LastSlaReminderDaysBefore.HasValue && dof.LastSlaReminderDaysBefore.Value <= threshold)
                    break; // bu ve daha yakın eşikler için zaten bildirim gönderilmiş

                dof.LastSlaReminderDaysBefore = threshold;

                await notifications.NotifyAsync(dof.AssignedToUserId!.Value,
                    $"DÖF #{dof.Id} termine {threshold} gün kaldı: {dof.Title}",
                    $"/Dofs/Details/{dof.Id}",
                    "schedule");

                if (dof.AssignedToUser != null)
                {
                    try
                    {
                        await emailService.SendEmailAsync(
                            dof.AssignedToUser.Email,
                            $"DÖF Termin Uyarısı: {dof.Title}",
                            $"Merhaba {dof.AssignedToUser.FullName},\n\n\"{dof.Title}\" başlıklı DÖF-#{dof.Id:D4} kaydının son tarihine {threshold} gün kaldı.\n\nDetaylar için sisteme giriş yapabilirsiniz.");
                    }
                    catch
                    {
                        // E-posta gönderim hatası hatırlatma akışını durdurmamalı
                    }
                }

                break;
            }
        }

        if (context.ChangeTracker.HasChanges())
            await context.SaveChangesAsync(stoppingToken);
    }

    private async Task SendEffectivenessRemindersAsync(AppDbContext context, INotificationService notifications,
        CancellationToken stoppingToken)
    {
        var now = DateTime.UtcNow;

        var candidates = await context.Dofs
            .Where(d => d.Status == DofStatus.Kapatildi
                && d.EffectivenessResult == EffectivenessResult.Beklemede
                && d.EffectivenessCheckDueDate.HasValue
                && d.EffectivenessCheckDueDate.Value <= now
                && d.EffectivenessReminderSentAt == null)
            .ToListAsync(stoppingToken);

        foreach (var dof in candidates)
        {
            dof.EffectivenessReminderSentAt = now;

            await notifications.NotifyManyAsync(
                new int?[] { dof.AssignedToUserId, dof.CreatedByUserId },
                $"DÖF #{dof.Id} için etkinlik kontrolü zamanı geldi: Alınan önlem kalıcı oldu mu?",
                $"/Dofs/Details/{dof.Id}",
                "fact_check");
        }

        if (context.ChangeTracker.HasChanges())
            await context.SaveChangesAsync(stoppingToken);
    }
}
