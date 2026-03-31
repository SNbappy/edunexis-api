using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace EduNexis.API.Controllers;

[Authorize(Roles = "Teacher,Admin")]
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
            var isAi = parsed.GetProperty("data").GetProperty("isHuman").GetInt32() == 0;
            var aiScore = parsed.GetProperty("data").GetProperty("fakePercentage").GetDouble();
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

    // ?? Web Plagiarism via Copyleaks ?????????????????????????????????????
    [HttpPost("analysis/check-web-plagiarism")]
    public async Task<IActionResult> CheckWebPlagiarism([FromBody] TextAnalysisRequest req, CancellationToken ct)
    {
        var email = _config["PlagiarismServices:CopyleaksEmail"];
        var apiKey = _config["PlagiarismServices:CopyleaksApiKey"];
        if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_COPYLEAKS_API_KEY")
            return Ok(new { success = false, message = "Copyleaks API key not configured." });

        try
        {
            var client = _http.CreateClient();
            // Step 1: Login to get token
            var loginBody = JsonSerializer.Serialize(new { email, key = apiKey });
            var loginRes = await client.PostAsync(
                "https://id.copyleaks.com/v3/account/login/api",
                new StringContent(loginBody, Encoding.UTF8, "application/json"), ct);
            var loginJson = JsonSerializer.Deserialize<JsonElement>(await loginRes.Content.ReadAsStringAsync(ct));
            var token = loginJson.GetProperty("access_token").GetString();

            // Step 2: Submit scan
            var scanId = Guid.NewGuid().ToString("N")[..12];
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var scanBody = JsonSerializer.Serialize(new {
                base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(req.Text)),
                filename = "submission.txt",
                properties = new { webhooks = new { status = "" } }
            });
            await client.PutAsync(
                $"https://api.copyleaks.com/v3/education/submit/file/{scanId}",
                new StringContent(scanBody, Encoding.UTF8, "application/json"), ct);

            return Ok(new {
                success = true,
                data = new {
                    scanId,
                    status = "submitted",
                    message = "Scan submitted to Copyleaks. Results take 20-60 seconds.",
                    checkUrl = $"https://app.copyleaks.com"
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = "Copyleaks error: " + ex.Message });
        }
    }
}

public record TextAnalysisRequest(string Text, string StudentName = "");
