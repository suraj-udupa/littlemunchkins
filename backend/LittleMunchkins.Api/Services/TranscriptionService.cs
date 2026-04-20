using System.Net.Http.Headers;

namespace LittleMunchkins.Api.Services;

public class TranscriptionService(IHttpClientFactory httpClientFactory, IConfiguration config)
{
    public async Task<string> TranscribeAsync(Stream audioStream, string filename)
    {
        var apiKey = config["OPENAI_API_KEY"] ?? throw new Exception("OPENAI_API_KEY not set");
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(audioStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/webm");
        content.Add(fileContent, "file", filename);
        content.Add(new StringContent("whisper-1"), "model");

        var resp = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", content);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<WhisperResponse>();
        return json?.Text ?? string.Empty;
    }

    private record WhisperResponse(string Text);
}
