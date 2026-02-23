using EduNexis.Domain.Interfaces.Services;

namespace EduNexis.Infrastructure.Services.Auth;

public class FirebaseAuthService : IFirebaseAuthService
{
    public Task<FirebaseTokenResult> VerifyTokenAsync(
        string idToken, CancellationToken ct = default)
        => Task.FromResult(new FirebaseTokenResult(
            "stub-uid",
            "stub@edunexis.com",
            "Stub User",
            new Dictionary<string, object>(),
            DateTime.UtcNow.AddHours(1)));

    public Task<FirebaseUserResult> GetUserAsync(
        string uid, CancellationToken ct = default)
        => Task.FromResult(new FirebaseUserResult(
            uid, "stub@edunexis.com", "Stub User", null, true));

    public Task RevokeTokenAsync(string uid, CancellationToken ct = default)
        => Task.CompletedTask;
}
