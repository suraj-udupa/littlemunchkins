using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LittleMunchkins.Api.Services;

public record BehaviorAnalysis(
    [property: JsonPropertyName("behavior_summary")] string BehaviorSummary,
    [property: JsonPropertyName("likely_cause")] string LikelyCause,
    [property: JsonPropertyName("age_appropriate")] bool AgeAppropriate,
    [property: JsonPropertyName("suggested_actions")] List<string> SuggestedActions,
    [property: JsonPropertyName("when_to_seek_help")] string? WhenToSeekHelp
);

public class ClaudeClient(IHttpClientFactory httpClientFactory, IConfiguration config)
{
    private const string Model = "claude-sonnet-4-6";
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";

    private static readonly string SystemPrompt = """
        You are a child development expert helping parents understand their child's behavior (ages 0–6).
        Analyse the provided input (text, image, audio transcript, or video frames + transcript) and respond ONLY with valid JSON matching this schema:
        {
          "behavior_summary": "string — what is happening",
          "likely_cause": "string — developmental or situational reason",
          "age_appropriate": true|false,
          "suggested_actions": ["string", ...],
          "when_to_seek_help": "string or null"
        }
        Be warm, practical, and evidence-based. Do not include any text outside the JSON object.
        """;

    public async Task<BehaviorAnalysis> AnalyseTextAsync(string question, string? childAge)
    {
        var userContent = childAge is not null
            ? $"Child age: {childAge}\n\n{question}"
            : question;
        return await SendAsync([new { type = "text", text = userContent }]);
    }

    public async Task<BehaviorAnalysis> AnalyseImageAsync(byte[] imageBytes, string mediaType, string? question, string? childAge)
    {
        var parts = new List<object>
        {
            new { type = "image", source = new { type = "base64", media_type = mediaType, data = Convert.ToBase64String(imageBytes) } },
            new { type = "text", text = BuildUserText(question, childAge) },
        };
        return await SendAsync(parts);
    }

    public async Task<BehaviorAnalysis> AnalyseTranscriptAsync(string transcript, string? question, string? childAge)
    {
        var text = $"Audio transcript:\n\"{transcript}\"\n\n{BuildUserText(question, childAge)}";
        return await SendAsync([new { type = "text", text }]);
    }

    public async Task<BehaviorAnalysis> AnalyseVideoFramesAsync(List<(byte[] Bytes, string MediaType)> frames, string transcript, string? question, string? childAge)
    {
        var parts = new List<object>();
        foreach (var (bytes, mt) in frames)
            parts.Add(new { type = "image", source = new { type = "base64", media_type = mt, data = Convert.ToBase64String(bytes) } });

        parts.Add(new { type = "text", text = $"Audio transcript:\n\"{transcript}\"\n\n{BuildUserText(question, childAge)}" });
        return await SendAsync(parts);
    }

    private static string BuildUserText(string? question, string? childAge)
    {
        var sb = new StringBuilder();
        if (childAge is not null) sb.AppendLine($"Child age: {childAge}");
        if (question is not null) sb.AppendLine(question);
        return sb.ToString().Trim();
    }

    private async Task<BehaviorAnalysis> SendAsync(IEnumerable<object> contentParts)
    {
        var apiKey = config["ANTHROPIC_API_KEY"] ?? throw new Exception("ANTHROPIC_API_KEY not set");
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var body = new
        {
            model = Model,
            max_tokens = 1024,
            system = SystemPrompt,
            messages = new[] { new { role = "user", content = contentParts } },
        };

        var json = JsonSerializer.Serialize(body);
        var resp = await client.PostAsync(ApiUrl, new StringContent(json, Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();

        var raw = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(raw);
        var text = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? throw new Exception("Empty Claude response");

        return JsonSerializer.Deserialize<BehaviorAnalysis>(text)
               ?? throw new Exception("Failed to parse Claude JSON");
    }
}
