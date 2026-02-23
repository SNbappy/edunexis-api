using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace EduNexis.Infrastructure.Services.Auth;

public class FirebaseAuthService : IFirebaseAuthService
{
    public FirebaseAuthService(IConfiguration configuration)
    {
        if (FirebaseApp.DefaultInstance is null)
        {
            var credentialPath = configuration["Firebase:CredentialPath"]!;
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(credentialPath)
            });
        }
    }

    public async Task<FirebaseTokenPayload?> VerifyTokenAsync(
        string idToken, CancellationToken ct = default)
    {
        try
        {
            var decoded = await FirebaseAuth.DefaultInstance
                .VerifyIdTokenAsync(idToken, ct);

            decoded.Claims.TryGetValue("name", out var name);

            return new FirebaseTokenPayload(
                decoded.Uid,
                decoded.Claims["email"].ToString()!,
                name?.ToString());
        }
        catch
        {
            return null;
        }
    }
}
