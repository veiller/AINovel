using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AINovel.Services;

public class GptService
{
    public async Task<string> GenerateAsync(string apiUrl, string apiKey, string prompt, string model = "gpt-3.5-turbo", int timeoutSeconds = 120, CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = prompt }
            },
            temperature = 0.7,
            stream = true
        };

        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        var result = new StringBuilder();

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                continue;

            var data = line["data: ".Length..];
            if (data == "[DONE]")
                break;

            try
            {
                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;

                if (root.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0 &&
                    choices[0].TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("content", out var content))
                {
                    result.Append(content.GetString());
                }
            }
            catch
            {
                // 忽略解析错误
            }
        }

        return result.ToString();
    }

    public async Task<bool> TestConnectionAsync(string apiUrl, string apiKey, string model = "gpt-3.5-turbo", int timeoutSeconds = 10)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        var requestBody = new
        {
            model,
            messages = new[] { new { role = "user", content = "Hi" } },
            max_tokens = 5
        };

        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}