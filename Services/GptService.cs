using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AINovel.Services;

public class GptService
{
    // 超时由每个请求的 CancellationTokenSource 单独控制，HttpClient 本身不做超时限制
    private static readonly HttpClient _httpClient = new() { Timeout = System.Threading.Timeout.InfiniteTimeSpan };

    public async Task<string> GenerateAsync(string apiUrl, string apiKey, string prompt, string model = "gpt-3.5-turbo", int timeoutSeconds = 120, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 2;
        var retryDelay = TimeSpan.FromSeconds(2);

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await GenerateOnceAsync(apiUrl, apiKey, prompt, model, timeoutSeconds, cancellationToken);
            }
            catch (OperationCanceledException) when (attempt < maxRetries)
            {
                Debug.WriteLine($"GPT API 超时(第{attempt + 1}次)，即将重试");
                await Task.Delay(retryDelay, cancellationToken);
                retryDelay *= 2;
            }
            catch (HttpRequestException) when (attempt < maxRetries)
            {
                Debug.WriteLine($"GPT API 网络错误(第{attempt + 1}次)，即将重试");
                await Task.Delay(retryDelay, cancellationToken);
                retryDelay *= 2;
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"GPT API 请求超时（{timeoutSeconds}秒），请检查网络或增大超时时间设置");
            }
        }
    }

    private static async Task<string> GenerateOnceAsync(string apiUrl, string apiKey, string prompt, string model, int timeoutSeconds, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var requestBody = new
        {
            model,
            messages = new[] { new { role = "user", content = prompt } },
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
