namespace EduNexis.Domain.Entities;

public class PasswordResetToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    protected PasswordResetToken() { }

    public static PasswordResetToken Create(Guid userId, string tokenHash, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId cannot be empty.");
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Token hash cannot be empty.");
        if (expiresAt <= DateTime.UtcNow)
            throw new DomainException("Expiry must be in the future.");

        return new PasswordResetToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
        };
    }

    public bool IsValid()
    {
        if (UsedAt.HasValue) return false;
        if (DateTime.UtcNow > ExpiresAt) return false;
        return true;
    }

    public void MarkUsed()
    {
        if (UsedAt.HasValue)
            throw new DomainException("Reset token has already been used.");
        UsedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}