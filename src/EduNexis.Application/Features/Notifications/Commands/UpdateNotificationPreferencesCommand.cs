using EduNexis.Application.Features.Notifications.Queries;

namespace EduNexis.Application.Features.Notifications.Commands;

public record NotificationPreferenceInput(string Type, bool InApp, bool Email, bool Sms);

public record UpdateNotificationPreferencesCommand(
    Guid UserId,
    List<NotificationPreferenceInput> Preferences
) : ICommand<ApiResponse>;

public sealed class UpdateNotificationPreferencesCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<UpdateNotificationPreferencesCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        UpdateNotificationPreferencesCommand cmd, CancellationToken ct)
    {
        var repo = uow.GetRepository<NotificationPreference>();
        var existing = (await repo.FindAsync(p => p.UserId == cmd.UserId, ct))
            .ToDictionary(p => p.Type);

        foreach (var input in cmd.Preferences ?? [])
        {
            if (!Enum.TryParse<NotificationType>(input.Type, out var type))
                continue; // Unknown type from an older or newer client — ignore.

            // A channel flag only means anything for types that use that
            // channel — otherwise the row would claim an SMS preference for a
            // type no SMS is ever sent for.
            var email = input.Email
                && GetNotificationPreferencesQueryHandler.EmailEligible.Contains(type);
            var sms = input.Sms
                && GetNotificationPreferencesQueryHandler.SmsEligible.Contains(type);

            if (existing.TryGetValue(type, out var pref))
            {
                pref.Set(input.InApp, email, sms);
                repo.Update(pref);
            }
            else
            {
                await repo.AddAsync(
                    NotificationPreference.Create(cmd.UserId, type, input.InApp, email, sms), ct);
            }
        }

        await uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Notification preferences saved.");
    }
}
