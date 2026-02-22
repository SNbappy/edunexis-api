namespace EduNexis.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    protected AuditLog() { }

    public static AuditLog Create(
        Guid? userId, string action, string entityType,
        string? entityId, string? oldValues,
        string? newValues, string? ipAddress) =>
        new()
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress
        };
}
