namespace EduNexis.Domain.Interfaces.Services;

public interface ISmsService
{
    /// <summary>
    /// Whether the platform actually has an SMS gateway configured.
    ///
    /// Exposed so the UI can say what is true rather than guessing. The settings
    /// page previously hard-coded "SMS is not available yet", which becomes a
    /// lie the moment a gateway is configured and nobody remembers to edit the
    /// string.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Sends one message. Returns true only if the gateway accepted it.
    /// Never throws — SMS is a side channel and must not break the caller.
    /// </summary>
    Task<bool> SendAsync(string phoneNumber, string message, CancellationToken ct = default);
}
