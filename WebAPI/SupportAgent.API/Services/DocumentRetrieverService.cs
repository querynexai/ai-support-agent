using Npgsql;
using Pgvector;
using SupportAgent.Api.Models;

namespace SupportAgent.Api.Services;

public class DocumentRetrieverService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly EmbeddingService _embeddings;

    public DocumentRetrieverService(NpgsqlDataSource dataSource, EmbeddingService embeddings)
    {
        _dataSource = dataSource;
        _embeddings = embeddings;
    }

    public async Task<List<SourceCitation>> RetrieveAsync(string query, int topK = 8)
    {
        var floatVector = await _embeddings.EmbedAsync(query);
        var queryVector = new Vector(floatVector);

        var results = new List<SourceCitation>();
        await using var conn = await _dataSource.OpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(
            @"SELECT d.document_id, d.title, c.content,
                 1 - (c.embedding <=> @q) AS similarity
          FROM document_chunks c
          JOIN documents d ON d.document_id = c.document_id
          ORDER BY c.embedding <=> @q
          LIMIT @k", conn);

        cmd.Parameters.AddWithValue("q", queryVector);
        cmd.Parameters.AddWithValue("k", topK);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new SourceCitation(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDouble(3)));
        }
        return results;
    }
}