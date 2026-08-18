using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace EduNexis.API.Controllers;

[Authorize(Roles = "Teacher,SuperAdmin")]
public class AnalysisController : BaseController
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _http;

    public AnalysisController(IConfiguration config, IHttpClientFactory http)
    {
        _config = config;
        _http = http;
    }

    // ?? AI Detection via ZeroGPT ?????????????????????????????????????????
    [HttpPost("analysis/detect-ai")]
    public async Task<IActionResult> DetectAI([FromBody] TextAnalysisRequest req, CancellationToken ct)
    {
        var apiKey = _config["PlagiarismServices:ZeroGptApiKey"];
        if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_ZEROGPT_API_KEY")
            return Ok(new { success = false, message = "ZeroGPT API key not configured." });

        try
        {
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Add("apiKey", apiKey);
            var body = JsonSerializer.Serialize(new { input_text = req.Text });
            var response = await client.PostAsync(
                "https://api.zerogpt.com/api/detect/detectText",
                new StringContent(body, Encoding.UTF8, "application/json"), ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);

            // ZeroGPT answers 200 with success:false and a null `data` for
            // account-level problems — an exhausted quota being the common one.
            // Reaching straight for data.fakePercentage turned that into
            // "requires an element of type 'Object'", which tells a teacher
            // nothing. Report what the provider actually said.
            if (!parsed.TryGetProperty("data", out var payload)
                || payload.ValueKind != JsonValueKind.Object)
            {
                var providerMessage = parsed.TryGetProperty("message", out var m)
                    ? m.GetString()
                    : null;
                return Ok(new
                {
                    success = false,
                    message = string.IsNullOrWhiteSpace(providerMessage)
                        ? "AI detection is unavailable right now."
                        : $"AI detection unavailable: {providerMessage}.",
                });
            }

            var isAi = payload.GetProperty("isHuman").GetInt32() == 0;
            var aiScore = payload.GetProperty("fakePercentage").GetDouble();
            return Ok(new {
                success = true,
                data = new {
                    aiScore = Math.Round(aiScore, 1),
                    humanScore = Math.Round(100 - aiScore, 1),
                    isAiGenerated = aiScore > 60,
                    level = aiScore > 80 ? "high" : aiScore > 40 ? "medium" : "low",
                    feedback = aiScore > 80 ? "Very likely AI-generated" :
                               aiScore > 40 ? "Possibly AI-assisted" : "Likely human-written"
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = "AI detection failed: " + ex.Message });
        }
    }

    /* Web-plagiarism lookup via Copyleaks was removed: it is a paid product the
       department is not licensing, so the endpoint could only ever return "not
       configured" while implying the platform offered the capability. Submission
       -to-submission similarity is unaffected — that runs client-side and needs
       no third party. */
}

public record TextAnalysisRequest(string Text, string StudentName = "");
