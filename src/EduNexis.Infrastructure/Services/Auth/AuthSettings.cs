using EduNexis.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace EduNexis.Infrastructure.Services.Auth;

public class AuthSettings : IAuthSettings
{
    public bool OtpRequired { get; }

    public AuthSettings(IConfiguration configuration)
    {
        OtpRequired = configuration.GetValue<bool>("Auth:OtpRequired", true);
    }
}