using EduNexis.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EduNexis.Infrastructure.Services.Sms;

/// <summary>
/// SMS over a generic HTTP gateway.
///
/// Deliberately not tied to one vendor. The bulk-SMS providers used in
/// Bangladesh (BulkSMSBD, SSL Wireless, MIM, Alpha Net and friends) all expose
/// the same shape: a GET or POST to an endpoint with an API key, a sender ID,
/// a number and the text. So the endpoint is a template configured per
/// deployment rather than a hard-coded URL, which means switching provider is
/// a config change and not a code change.
///
/// Configure with:
///   Sms:Enabled   true|false
///   Sms:Endpoint  https://bulksmsbd.net/api/smsapi?api_key={apiKey}&senderid={senderId}&number={number}&message={message}
///   Sms:ApiKey    your key
///   Sms:SenderId  your approved sender id
///   Sms:Method    GET|POST      (default GET — what most of these gateways use)
///
/// With Sms:Enabled unset or false, IsConfigured is false, nothing is sent, and
/// the UI shows SMS as unavailable rather than pretending to deliver.
/// </summary>
public class SmsService : ISmsService
{
    private readonly HttpClient _http;
    private readonly ILogger<SmsService> _logger;
    private readonly bool _enabled;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _senderId;
    private readonly string _method;

    public SmsService(
        IHttpClientFactory httpFactory,
        ILogger<SmsService> logger,
        IConfiguration configuration)
    {
        _http = httpFactory.CreateClient("sms-gateway");
        _http.Timeout = TimeSpan.FromSeconds(15);

        _logger   = logger;
        _enabled  = configuration.GetValue<bool>("Sms:Enabled", false);
        _endpoint = configuration["Sms:Endpoint"] ?? "";
        _apiKey   = configuration["Sms:ApiKey"]   ?? "";
        _senderId = configuration["Sms:SenderId"] ?? "";
        _method   = (configuration["Sms:Method"]  ?? "GET").ToUpperInvariant();
    }

    public bool IsConfigured =>
        _enabled
        && !string.IsNullOrWhiteSpace(_endpoint)
        && !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<bool> SendAsync(
        string phoneNumber, string message, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("SMS not configured. Skipping send to {Phone}.", Mask(phoneNumber));
            return false;
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            _logger.LogWarning("SMS requested with no phone number. Skipping.");
            return false;
        }

        try
        {
            var url = _endpoint
                .Replace("{apiKey}",   Uri.EscapeDataString(_apiKey))
                .Replace("{senderId}", Uri.EscapeDataString(_senderId))
                .Replace("{number}",   Uri.EscapeDataString(Normalise(phoneNumber)))
                .Replace("{message}",  Uri.EscapeDataString(message));

            using var request = new HttpRequestMessage(
                _method == "POST" ? HttpMethod.Post : HttpMethod.Get, url);

            using var response = await _http.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("SMS sent to {Phone}", Mask(phoneNumber));
                return true;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "SMS gateway rejected message to {Phone}: HTTP {Status} {Body}",
                Mask(phoneNumber), (int)response.StatusCode, Truncate(body));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {Phone}", Mask(phoneNumber));
            return false;
        }
    }

    /// <summary>
    /// Strips spaces and dashes people type into the profile field, and converts
    /// a local 01XXXXXXXXX into the 8801XXXXXXXXX these gateways expect.
    /// </summary>
    private static string Normalise(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00")) digits = digits[2..];
        if (digits.StartsWith("01") && digits.Length == 11) digits = "88" + digits;
        return digits;
    }

    /// <summary>Never log a full number — it is personal data and ends up in log files.</summary>
    private static string Mask(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "(none)";
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length <= 4 ? "****" : "****" + digits[^4..];
    }

    private static string Truncate(string s) =>
        string.IsNullOrEmpty(s) ? "" : (s.Length <= 200 ? s : s[..200] + "…");
}
