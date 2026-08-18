using EduNexis.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace EduNexis.Application.Features.Notifications.Commands;

public record NotificationDto(
    Guid Id, string Title, string Body,
    string Type, bool IsRead, string? RedirectUrl, DateTime CreatedAt
);

public record SendNotificationCommand(
    Guid UserId,
    string Title,
    string Body,
    NotificationType Type,
    string? RedirectUrl = null
) : ICommand<ApiResponse>;

public sealed class SendNotificationCommandHandler(
    IUnitOfWork uow,
    IEmailService emailService,
    ISmsService smsService,
    IEmailTemplateBuilder templateBuilder,
    ILogger<SendNotificationCommandHandler> logger
) : ICommandHandler<SendNotificationCommand, ApiResponse>
{
    /// <summary>
    /// Notification types that may ALSO send an email — every type.
    ///
    /// This used to be a curated subset, which meant the settings page showed a
    /// dash rather than a toggle for half the list: a user who wanted an email
    /// for announcements simply could not ask for one. Whether a given email is
    /// worth receiving is the user's judgement, and the per-type switch already
    /// records it — the platform's job is only to honour that switch.
    /// </summary>
    public static readonly HashSet<NotificationType> EmailEligibleTypes =
        Enum.GetValues<NotificationType>().ToHashSet();

    /// <summary>
    /// Types worth an SMS.
    ///
    /// A much shorter list than email on purpose: every message costs money and
    /// interrupts someone's phone, so this is limited to things with a deadline
    /// or a result attached — the cases where being told hours earlier actually
    /// changes what a student does.
    /// </summary>
    public static readonly HashSet<NotificationType> SmsEligibleTypes = new()
    {
        NotificationType.NewAssignment,
        NotificationType.AssignmentUpdated,
        NotificationType.AssignmentDeadlineReminder,
        NotificationType.MarksPublished,
        NotificationType.AssignmentGraded,
    };

    public async ValueTask<ApiResponse> Handle(
        SendNotificationCommand command, CancellationToken ct)
    {
        // 0) What has this user asked for?
        //
        // A missing row means "on", so someone who has never opened Settings —
        // and any notification type added after they last did — keeps receiving
        // everything. Only an explicit opt-out silences anything.
        var pref = (await uow.GetRepository<NotificationPreference>()
                .FindAsync(p => p.UserId == command.UserId && p.Type == command.Type, ct))
            .FirstOrDefault();

        // Defaults differ by channel, and deliberately so:
        //   in-app  on   — the product's own surface, costs nothing
        //   email   on   — the address is a verified university one and course
        //                  mail is expected; students were missing deadlines
        //                  because email defaulted to off and nobody found the
        //                  setting. Still one switch away from silence.
        //   sms     off  — opt-in, costs money and needs a phone number
        var wantsInApp = pref?.InApp ?? true;
        var wantsEmail = pref?.Email ?? true;
        var wantsSms   = pref?.Sms   ?? false;

        // 1) Save in-app notification
        if (wantsInApp)
        {
            var notification = Notification.Create(
                command.UserId, command.Title, command.Body,
                command.Type, command.RedirectUrl);

            await uow.GetRepository<Notification>().AddAsync(notification, ct);
            await uow.SaveChangesAsync(ct);
        }

        // 2) Fire-and-forget email if this type is email-eligible and wanted
        if (wantsEmail && EmailEligibleTypes.Contains(command.Type))
        {
            await SendEmailAsync(command, ct);
        }

        // 3) SMS, same rules. Silently a no-op when no gateway is configured.
        if (wantsSms && SmsEligibleTypes.Contains(command.Type))
        {
            await SendSmsAsync(command, ct);
        }

        return ApiResponse.Ok("Notification sent.");
    }

    /// <summary>
    /// One short SMS. No links, no HTML — these are read on a lock screen and
    /// every extra character can cost another message segment.
    /// </summary>
    private async Task SendSmsAsync(SendNotificationCommand command, CancellationToken ct)
    {
        try
        {
            if (!smsService.IsConfigured) return;

            var user = await uow.Users.GetWithProfileAsync(command.UserId, ct);
            var phone = user?.Profile?.PhoneNumber;

            if (string.IsNullOrWhiteSpace(phone))
            {
                logger.LogDebug(
                    "SMS wanted but no phone number on profile (UserId={UserId}, Type={Type})",
                    command.UserId, command.Type);
                return;
            }

            var text = $"EduNexis: {command.Title}. {command.Body}";
            if (text.Length > 300) text = text[..297] + "...";

            var sent = await smsService.SendAsync(phone, text, ct);

            if (!sent)
                logger.LogWarning(
                    "Notification SMS NOT delivered (UserId={UserId}, Type={Type}). "
                    + "The in-app notification was still saved.",
                    command.UserId, command.Type);
        }
        catch (Exception ex)
        {
            // SMS must never break the notification flow.
            logger.LogError(ex,
                "Failed to send SMS for notification (UserId={UserId}, Type={Type})",
                command.UserId, command.Type);
        }
    }

    private async Task SendEmailAsync(SendNotificationCommand command, CancellationToken ct)
    {
        try
        {
            var user = await uow.Users.GetByIdAsync(command.UserId, ct);
            if (user is null || string.IsNullOrWhiteSpace(user.Email))
            {
                logger.LogWarning(
                    "Skipping email for notification: user {UserId} not found or has no email.",
                    command.UserId);
                return;
            }

            var bodyHtml = "<p>" + EscapeHtml(command.Body) + "</p>";

            if (!string.IsNullOrWhiteSpace(command.RedirectUrl))
            {
                var fullUrl = command.RedirectUrl.StartsWith("http")
                    ? command.RedirectUrl
                    : templateBuilder.FrontendBaseUrl + command.RedirectUrl;
                bodyHtml += templateBuilder.Button(fullUrl, "Open in EduNexis");
            }

            bodyHtml +=
                "<p style=\"color:#78716c;font-size:13px;margin-top:24px;\">" +
                "You are receiving this because you have an account on EduNexis. " +
                // No longer "coming soon" — Settings > Notifications ships now.
                "You can choose which notifications you receive in Settings > Notifications." +
                "</p>";

            var html = templateBuilder.Build(command.Title, bodyHtml);

            // Same reason as the auth flows: a rejected send returns false
            // rather than throwing, so discarding the result made a failed
            // notification email indistinguishable from a delivered one.
            var sent = await emailService.SendAsync(user.Email, command.Title, html, ct);

            if (sent)
                logger.LogInformation(
                    "Notification email sent (UserId={UserId}, Type={Type})",
                    command.UserId, command.Type);
            else
                logger.LogWarning(
                    "Notification email NOT delivered - the provider rejected it "
                    + "(UserId={UserId}, Type={Type}). The in-app notification was still saved.",
                    command.UserId, command.Type);
        }
        catch (Exception ex)
        {
            // Email failure must NEVER break the in-app notification flow
            logger.LogError(ex,
                "Failed to send email for notification (UserId={UserId}, Type={Type})",
                command.UserId, command.Type);
        }
    }

    private static string EscapeHtml(string s) =>
        s.Replace("&", "&amp;")
         .Replace("<", "&lt;")
         .Replace(">", "&gt;")
         .Replace("\"", "&quot;");
}

public record GetMyNotificationsQuery(Guid UserId)
    : IQuery<ApiResponse<List<NotificationDto>>>;

public sealed class GetMyNotificationsQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetMyNotificationsQuery, ApiResponse<List<NotificationDto>>>
{
    public async ValueTask<ApiResponse<List<NotificationDto>>> Handle(
        GetMyNotificationsQuery query, CancellationToken ct)
    {
        var notifications = await uow.GetRepository<Notification>()
            .FindAsync(n => n.UserId == query.UserId, ct);

        var dtos = notifications
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.Body, n.Type.ToString(),
                n.IsRead, n.RedirectUrl, n.CreatedAt))
            .ToList();

        return ApiResponse<List<NotificationDto>>.Ok(dtos);
    }
}