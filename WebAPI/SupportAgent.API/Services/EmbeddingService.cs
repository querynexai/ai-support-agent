using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SupportAgent.Api.Services;

public class EmbeddingService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private const string ModelUrl =
        "https://router.huggingface.co/hf-inference/models/sentence-transformers/all-MiniLM-L6-v2/pipeline/feature-extraction";

    public EmbeddingService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["HuggingFace:ApiKey"]
            ?? throw new InvalidOperationException("HuggingFace:ApiKey not set");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<float[]> EmbedAsync(string text)
    {
        var payload = new { inputs = text, options = new { wait_for_model = true } };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _http.PostAsync(ModelUrl, content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"HuggingFace error ({response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var vectorArray = root.ValueKind == JsonValueKind.Array && root[0].ValueKind == JsonValueKind.Array
            ? root[0]
            : root;

        var vector = new float[vectorArray.GetArrayLength()];
        for (int i = 0; i < vector.Length; i++)
            vector[i] = vectorArray[i].GetSingle();

        return vector;
    }

    public async Task<List<float[]>> EmbedBatchAsync(IEnumerable<string> texts)
    {
        var result = new List<float[]>();
        foreach (var t in texts)
        {
            result.Add(await EmbedAsync(t));
            await Task.Delay(100);
        }
        return result;
    }
}