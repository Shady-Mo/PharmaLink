using System.Net.Http.Headers;
using Infrastructure.AI.Models;
using Infrastructure.AI.Options;

namespace Infrastructure.AI.Services;

public class TranscriptionService
{
    private readonly HttpClient _httpClient;
    private readonly GroqOptions _options;

    public TranscriptionService(HttpClient httpClient, IOptions<AiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.Providers.Groq;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<string> TranscribeAudioAsync(Stream audioStream, string fileName)
    {
        if (!_options.Models.TryGetValue(nameof(ModelRole.Transcription), out var models) || models.Length == 0)
        {
            throw new InvalidOperationException("Transcription model not configured.");
        }

        var modelId = models[0];

        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(audioStream);
        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/mpeg");
        content.Add(streamContent, "file", fileName);
        content.Add(new StringContent(modelId), "model");

        var response = await _httpClient.PostAsync("audio/transcriptions", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();

        try
        {
            var jsonDoc = JsonDocument.Parse(result);
            if (jsonDoc.RootElement.TryGetProperty("text", out var textElement))
            {
                return textElement.GetString() ?? result;
            }
        }
        catch
        {
            // fallback to returning raw string
        }

        return result;
    }
}