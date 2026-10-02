using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SupportAgent.Api.Models;

namespace SupportAgent.Api.Services;

public class ChatOrchestratorService
{
    private readonly HttpClient _http;
    private readonly DocumentRetrieverService _retriever;
    private readonly CustomerDataService _customerData;
    private readonly string _apiKey;
    private readonly string _model;

    public ChatOrchestratorService(
        HttpClient http,
        DocumentRetrieverService retriever,
        CustomerDataService customerData,
        IConfiguration config)
    {
        _http = http;
        _retriever = retriever;
        _customerData = customerData;
        _apiKey = config["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey not set");
        _model = config["Groq:Model"] ?? "openai/gpt-oss-120b";

        _http.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<ChatResponse> RespondAsync(ChatRequest request)
    {
        // 1. Retrieve document context
        var sources = await _retriever.RetrieveAsync(request.Message, topK: 4);

        // 2. Try to extract order ID from message (simple heuristic)
        object? dataContext = null;
        var orderIdMatch = System.Text.RegularExpressions.Regex.Match(request.Message, @"#?(\d{1,6})");
        if (orderIdMatch.Success && int.TryParse(orderIdMatch.Groups[1].Value, out var orderId))
        {
            var orderStatus = await _customerData.GetOrderStatusAsync(orderId);
            if (orderStatus != null) dataContext = new { order = orderStatus };
        }

        // 3. Build prompt
        var docContext = sources.Count > 0
            ? string.Join("\n\n", sources.Select((s, i) => $"[{i + 1}] ({s.Title}): {s.Snippet}"))
            : "No relevant documents found.";

        var dataContextStr = dataContext != null
            ? JsonSerializer.Serialize(dataContext)
            : "No live data available.";

        var systemPrompt = $"""
            You are QueryNex's friendly customer support agent.
            Answer the customer's question using the context below.
            Be concise, warm, and helpful. Use bullet points for multi-step answers.
            If citing documents, reference them as [1], [2], etc.
            If the context does not contain the answer, say so honestly and suggest contacting support@querynex.ai.

            DOCUMENT CONTEXT:
            {docContext}

            LIVE DATA CONTEXT:
            {dataContextStr}

            RULES:
            - Never make up order details or policy specifics.
            - Keep responses under 200 words unless a step-by-step answer is required.
            - End with a friendly offer to help further.
            """;

        // 4. Call Groq
        var payload = new
        {
            model = _model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = request.Message }
            },
            temperature = 0.3
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("chat/completions", content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return new ChatResponse("", request.SessionId ?? Guid.NewGuid(), sources, false, $"Groq error: {body}");

        using var doc = JsonDocument.Parse(body);
        var answer = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
            ?? "Sorry, I could not generate an answer.";

        // 5. Escalate if no good sources AND no data
        var escalated = sources.Count == 0 && dataContext == null;

        return new ChatResponse(answer, request.SessionId ?? Guid.NewGuid(), sources, escalated);
    }
}