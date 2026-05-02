using EduNexis.Domain.Interfaces.Services;

namespace EduNexis.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiry { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsProfileComplete { get; private set; } = false;

    // Email verification (G4)
    public bool IsEmailVerified { get; private set; } = false;
    public string? EmailVerificationOtpHash { get; private set; }
    public DateTime? EmailVerificationOtpExpiresAt { get; private set; }
    public DateTime? LastOtpSentAt { get; private set; }

    // Navigation
    public UserProfile? Profile { get; private set; }
    public ICollection<CourseMember> CourseMembers { get; private set; } = [];
    public ICollection<TeacherQuota> TeacherQuotas { get; private set; } = [];
    public ICollection<Notification> Notifications { get; private set; } = [];
    public ICollection<UserEducation> Educations { get; private set; } = [];
    public ICollection<UserPublication> Publications { get; private set; } = [];

    protected User() { }

    public static User Create(string email, string passwordHash, UserRole role)
    {
        ValidateEmailMatchesRole(email, role);
        return new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role
        };
    }

    /* ── Refresh token ──────────────────────────────────── */

    public void SetRefreshToken(string token, DateTime expiry)
    {
        RefreshToken = token;
        RefreshTokenExpiry = expiry;
        SetUpdatedAt();
    }

    public void ClearRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiry = null;
        SetUpdatedAt();
    }

    public bool IsRefreshTokenValid(string token) =>
        RefreshToken == token &&
        RefreshTokenExpiry.HasValue &&
        RefreshTokenExpiry.Value > DateTime.UtcNow;

    /* ── Password ───────────────────────────────────────── */

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("Password hash cannot be empty.");
        PasswordHash = newPasswordHash;
        SetUpdatedAt();
    }

    /* ── Email verification (G4) ────────────────────────── */

    /// <summary>
    /// Sets a new OTP hash with expiry. Also updates LastOtpSentAt for resend cooldown tracking.
    /// </summary>
    public void SetEmailOtp(string otpHash, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(otpHash))
            throw new DomainException("OTP hash cannot be empty.");
        if (expiresAt <= DateTime.UtcNow)
            throw new DomainException("OTP expiry must be in the future.");

        EmailVerificationOtpHash = otpHash;
        EmailVerificationOtpExpiresAt = expiresAt;
        LastOtpSentAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    /// <summary>
    /// Returns true if cooldown has elapsed since last OTP send.
    /// </summary>
    public bool CanResendOtp(int cooldownSeconds = 60)
    {
        if (LastOtpSentAt is null) return true;
        return (DateTime.UtcNow - LastOtpSentAt.Value).TotalSeconds >= cooldownSeconds;
    }

    /// <summary>
    /// Returns seconds remaining until next OTP can be sent (0 if ready).
    /// </summary>
    public int OtpResendWaitSeconds(int cooldownSeconds = 60)
    {
        if (LastOtpSentAt is null) return 0;
        var elapsed = (DateTime.UtcNow - LastOtpSentAt.Value).TotalSeconds;
        var remaining = cooldownSeconds - (int)elapsed;
        return remaining > 0 ? remaining : 0;
    }

    /// <summary>
    /// Verifies a provided OTP. On success: marks email verified, clears OTP fields, returns true.
    /// On any failure (no OTP set, expired, mismatch): returns false without state change.
    /// </summary>
    public bool TryConsumeOtp(string providedOtp, IPasswordHasher hasher)
    {
        if (string.IsNullOrWhiteSpace(providedOtp)) return false;
        if (string.IsNullOrWhiteSpace(EmailVerificationOtpHash)) return false;
        if (EmailVerificationOtpExpiresAt is null) return false;
        if (DateTime.UtcNow > EmailVerificationOtpExpiresAt) return false;
        if (!hasher.Verify(providedOtp, EmailVerificationOtpHash)) return false;

        IsEmailVerified = true;
        EmailVerificationOtpHash = null;
        EmailVerificationOtpExpiresAt = null;
        SetUpdatedAt();
        return true;
    }

    /// <summary>
    /// Marks user as email-verified without OTP check.
    /// Used for Firebase users (Google has already verified the email)
    /// and for grandfathering existing accounts.
    /// </summary>
    public void MarkEmailVerified()
    {
        if (IsEmailVerified) return;
        IsEmailVerified = true;
        EmailVerificationOtpHash = null;
        EmailVerificationOtpExpiresAt = null;
        SetUpdatedAt();
    }

    /* ── Profile / state ────────────────────────────────── */

    public void MarkProfileComplete() { IsProfileComplete = true; SetUpdatedAt(); }
    public void MarkProfileIncomplete() { IsProfileComplete = false; SetUpdatedAt(); }
    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
    public void Activate() { IsActive = true; SetUpdatedAt(); }

    /* ── Validation ─────────────────────────────────────── */

    private static void ValidateEmailMatchesRole(string email, UserRole role)
    {
        if (role == UserRole.Teacher &&
            !email.EndsWith("@just.edu.bd", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Teacher email must end with @just.edu.bd");

        if (role == UserRole.Student &&
            !email.EndsWith("@student.just.edu.bd", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Student email must end with @student.just.edu.bd");
    }
}