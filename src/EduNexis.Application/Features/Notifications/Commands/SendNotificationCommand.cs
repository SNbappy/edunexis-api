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
    IEmailTemplateBuilder templateBuilder,
    ILogger<SendNotificationCommandHandler> logger
) : ICommandHandler<SendNotificationCommand, ApiResponse>
{
    /// <summary>
    /// Notification types that should ALSO send an email.
    /// Lower-noise types (announcements, generic) stay in-app only.
    /// </summary>
    private static readonly HashSet<NotificationType> EmailEligibleTypes = new()
    {
        NotificationType.NewAssignment,
        NotificationType.MarksPublished,
        NotificationType.AssignmentDeadlineReminder,
        NotificationType.JoinRequestReceived,
        NotificationType.CourseJoinApproved,
        NotificationType.CourseJoinRejected,
        NotificationType.GradeComplaint,
    };

    public async ValueTask<ApiResponse> Handle(
        SendNotificationCommand command, CancellationToken ct)
    {
        // 1) Save in-app notification (existing behavior)
        var notification = Notification.Create(
            command.UserId, command.Title, command.Body,
            command.Type, command.RedirectUrl);

        await uow.GetRepository<Notification>().AddAsync(notification, ct);
        await uow.SaveChangesAsync(ct);

        // 2) Fire-and-forget email if this notification type is email-eligible
        if (EmailEligibleTypes.Contains(command.Type))
        {
            await SendEmailAsync(command, ct);
        }

        return ApiResponse.Ok("Notification sent.");
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
                "Notification preferences can be managed from your account settings (coming soon)." +
                "</p>";

            var html = templateBuilder.Build(command.Title, bodyHtml);

            await emailService.SendAsync(user.Email, command.Title, html, ct);
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