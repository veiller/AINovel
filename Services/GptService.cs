using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AINovel.Services;

public class GptService
{
    private static readonly HttpClient _httpClient = new();
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<string> GenerateAsync(string apiUrl, string apiKey, string prompt, string model = "gpt-3.5-turbo", double temperature = 0.7, int timeoutSeconds = 120, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 2;
        var retryDelay = TimeSpan.FromSeconds(2);

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await GenerateOnceAsync(apiUrl, apiKey, prompt, model, temperature, timeoutSeconds, cancellationToken);
            }
            catch (Exception ex) when (attempt < maxRetries && IsRetryable(ex))
            {
                Debug.WriteLine($"GPT API 调用失败(第{attempt + 1}次)，即将重试: {ex.Message}");
                await Task.Delay(retryDelay, cancellationToken);
                retryDelay = TimeSpan.FromSeconds(retryDelay.TotalSeconds * 2); // 指数退避
            }
        }

        // 最后一次尝试，失败直接向上抛
        return await GenerateOnceAsync(apiUrl, apiKey, prompt, model, temperature, timeoutSeconds, cancellationToken);
    }

    private static bool IsRetryable(Exception ex) => ex switch
    {
        HttpRequestException => true,
        TaskCanceledException => true,
        _ => false
    };

    private static async Task<string> GenerateOnceAsync(string apiUrl, string apiKey, string prompt, string model, double temperature, int timeoutSeconds, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = prompt }
            },
            temperature,
            stream = true
        };

        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        var result = new StringBuilder();

        while (!reader.EndOfStream)
        {
            cts.Token.ThrowIfCancellationRequested();

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
            catch (JsonException ex)
            {
                Debug.WriteLine($"SSE 数据解析异常: {ex.Message}, data: {data[..Math.Min(data.Length, 200)]}");
            }
        }

        return result.ToString();
    }

    public async Task<bool> TestConnectionAsync(string apiUrl, string apiKey, string model = "gpt-3.5-turbo", int timeoutSeconds = 10)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

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
            var response = await _httpClient.SendAsync(request, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
