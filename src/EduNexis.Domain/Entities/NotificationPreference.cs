namespace EduNexis.Domain.Entities;

/// <summary>
/// Which notifications a user wants, per type and per channel.
///
/// One row per user per type, and a *missing* row means "on". That is the whole
/// design: everything is on by default, so a new notification type does not
/// silently arrive switched off for every existing user, and a user who has
/// never opened Settings has nothing stored at all. Rows only exist for choices
/// somebody actually made.
/// </summary>
public class NotificationPreference : BaseEntity
{
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }

    /// <summary>Show it in the bell / notifications list.</summary>
    public bool InApp { get; private set; } = true;

    /// <summary>Also email it, for the types that are email-eligible.</summary>
    public bool Email { get; private set; } = true;

    public User User { get; private set; } = null!;

    protected NotificationPreference() { }

    public static NotificationPreference Create(
        Guid userId, NotificationType type, bool inApp, bool email)
        => new()
        {
            UserId = userId,
            Type = type,
            InApp = inApp,
            Email = email,
        };

    public void Set(bool inApp, bool email)
    {
        InApp = inApp;
        Email = email;
        SetUpdatedAt();
    }
}
