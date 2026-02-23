namespace EduNexis.Domain.Interfaces.Services;

public interface IFirebaseAuthService
{
    Task<FirebaseTokenResult> VerifyTokenAsync(string idToken, CancellationToken ct = default);
    Task<FirebaseUserResult> GetUserAsync(string uid, CancellationToken ct = default);
    Task RevokeTokenAsync(string uid, CancellationToken ct = default);
}

public sealed record FirebaseTokenResult(
    string Uid,
    string Email,
    string? Name,
    Dictionary<string, object> Claims,
    DateTime ExpiryTime);

public sealed record FirebaseUserResult(
    string Uid,
    string Email,
    string? DisplayName,
    string? PhotoUrl,
    bool EmailVerified);
