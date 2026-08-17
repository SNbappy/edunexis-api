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

    /// <summary>
    /// Also email it, for the types that are email-eligible.
    ///
    /// Off by default. Email is the channel people complain about first, and a
    /// platform that starts mailing every new material to every student reads
    /// as spam — so it is opt-in, while in-app stays on.
    /// </summary>
    public bool Email { get; private set; } = false;

    /// <summary>
    /// Also send an SMS, for types that are SMS-eligible.
    ///
    /// Off by default: it costs money per message and needs a phone number on
    /// the profile, so it is never something a user is opted into silently.
    /// </summary>
    public bool Sms { get; private set; } = false;

    public User User { get; private set; } = null!;

    protected NotificationPreference() { }

    public static NotificationPreference Create(
        Guid userId, NotificationType type, bool inApp, bool email, bool sms)
        => new()
        {
            UserId = userId,
            Type = type,
            InApp = inApp,
            Email = email,
            Sms = sms,
        };

    public void Set(bool inApp, bool email, bool sms)
    {
        InApp = inApp;
        Email = email;
        Sms = sms;
        SetUpdatedAt();
    }
}
