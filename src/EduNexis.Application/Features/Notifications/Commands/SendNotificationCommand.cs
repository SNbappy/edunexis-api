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
    IUnitOfWork uow
) : ICommandHandler<SendNotificationCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        SendNotificationCommand command, CancellationToken ct)
    {
        var notification = Notification.Create(
            command.UserId, command.Title, command.Body,
            command.Type, command.RedirectUrl);

        await uow.GetRepository<Notification>().AddAsync(notification, ct);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Notification sent.");
    }
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
