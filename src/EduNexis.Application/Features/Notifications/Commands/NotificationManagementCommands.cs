namespace EduNexis.Application.Features.Notifications.Commands;

// -- Mark single read ----------------------------------------------
public record MarkNotificationReadCommand(Guid NotificationId, Guid UserId)
    : ICommand<ApiResponse>;

public sealed class MarkNotificationReadCommandHandler(IUnitOfWork uow)
    : ICommandHandler<MarkNotificationReadCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        MarkNotificationReadCommand cmd, CancellationToken ct)
    {
        var repo = uow.GetRepository<Notification>();
        var n = await repo.FirstOrDefaultAsync(
            x => x.Id == cmd.NotificationId && x.UserId == cmd.UserId, ct);

        if (n is null) return ApiResponse.Fail("Notification not found.");

        n.MarkAsRead();
        repo.Update(n);
        await uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Marked as read.");
    }
}

// -- Mark all read -------------------------------------------------
public record MarkAllNotificationsReadCommand(Guid UserId) : ICommand<ApiResponse>;

public sealed class MarkAllNotificationsReadCommandHandler(IUnitOfWork uow)
    : ICommandHandler<MarkAllNotificationsReadCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        MarkAllNotificationsReadCommand cmd, CancellationToken ct)
    {
        var repo = uow.GetRepository<Notification>();
        var notifications = await repo.FindAsync(
            n => n.UserId == cmd.UserId && !n.IsRead, ct);

        foreach (var n in notifications)
        {
            n.MarkAsRead();
            repo.Update(n);
        }

        await uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("All notifications marked as read.");
    }
}

// -- Delete --------------------------------------------------------
public record DeleteNotificationCommand(Guid NotificationId, Guid UserId)
    : ICommand<ApiResponse>;

public sealed class DeleteNotificationCommandHandler(IUnitOfWork uow)
    : ICommandHandler<DeleteNotificationCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        DeleteNotificationCommand cmd, CancellationToken ct)
    {
        var repo = uow.GetRepository<Notification>();
        var n = await repo.FirstOrDefaultAsync(
            x => x.Id == cmd.NotificationId && x.UserId == cmd.UserId, ct);

        if (n is null) return ApiResponse.Fail("Notification not found.");

        repo.Delete(n);
        await uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Notification deleted.");
    }
}
