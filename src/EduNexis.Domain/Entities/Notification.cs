namespace EduNexis.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public bool IsRead { get; private set; } = false;
    public string? RedirectUrl { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    protected Notification() { }

    public static Notification Create(
        Guid userId, string title, string body,
        NotificationType type, string? redirectUrl = null) =>
        new()
        {
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            RedirectUrl = redirectUrl
        };

    public void MarkAsRead() { IsRead = true; SetUpdatedAt(); }
}
