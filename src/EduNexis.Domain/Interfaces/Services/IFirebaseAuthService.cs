namespace EduNexis.Domain.Interfaces.Services;

public record FirebaseTokenPayload(string Uid, string Email, string? Name);

public interface IFirebaseAuthService
{
    Task<FirebaseTokenPayload?> VerifyTokenAsync(string idToken, CancellationToken ct = default);
}
