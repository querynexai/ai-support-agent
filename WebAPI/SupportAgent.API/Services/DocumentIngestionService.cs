using Npgsql;
using Pgvector;
using SupportAgent.Api.Models;

namespace SupportAgent.Api.Services;

public class DocumentIngestionService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly EmbeddingService _embeddings;

    public DocumentIngestionService(NpgsqlDataSource dataSource, EmbeddingService embeddings)
    {
        _dataSource = dataSource;
        _embeddings = embeddings;
    }

    public async Task<IngestResult> IngestAsync(DocumentUploadRequest request)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync();

            // 1. Insert document
            int documentId;
            await using (var cmd = new NpgsqlCommand(
                "INSERT INTO documents (title, source_type, content, source_url) " +
                "VALUES (@t, @st, @c, @su) RETURNING document_id", conn))
            {
                cmd.Parameters.AddWithValue("t", request.Title);
                cmd.Parameters.AddWithValue("st", request.SourceType);
                cmd.Parameters.AddWithValue("c", request.Content);
                cmd.Parameters.AddWithValue("su", (object?)request.SourceUrl ?? DBNull.Value);
                documentId = (int)(await cmd.ExecuteScalarAsync())!;
            }

            // 2. Chunk the text
            var chunks = ChunkText(request.Content, maxChars: 500, overlap: 80);

            // 3. Embed each chunk and store
            for (int i = 0; i < chunks.Count; i++)
            {
                var floatVector = await _embeddings.EmbedAsync(chunks[i]);
                var vector = new Vector(floatVector);   // <-- wrap in Pgvector.Vector

                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO document_chunks (document_id, chunk_index, content, embedding, token_count) " +
                    "VALUES (@d, @i, @c, @e, @tc)", conn);

                cmd.Parameters.AddWithValue("d", documentId);
                cmd.Parameters.AddWithValue("i", i);
                cmd.Parameters.AddWithValue("c", chunks[i]);
                cmd.Parameters.AddWithValue("e", vector);   // <-- Vector object, not float[]
                cmd.Parameters.AddWithValue("tc", chunks[i].Split(' ').Length);

                await cmd.ExecuteNonQueryAsync();
            }

            return new IngestResult(documentId, chunks.Count, true);
        }
        catch (Exception ex)
        {
            return new IngestResult(0, 0, false, ex.Message);
        }
    }

    public async Task<List<DocumentResponse>> ListDocumentsAsync()
    {
        var results = new List<DocumentResponse>();
        await using var conn = await _dataSource.OpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(
            @"SELECT d.document_id, d.title, d.source_type,
                 COUNT(c.chunk_id) AS chunk_count,
                 d.uploaded_at
          FROM documents d
          LEFT JOIN document_chunks c ON c.document_id = d.document_id
          GROUP BY d.document_id, d.title, d.source_type, d.uploaded_at
          ORDER BY d.uploaded_at DESC", conn);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new DocumentResponse(
                reader.GetInt32(reader.GetOrdinal("document_id")),
                reader.GetString(reader.GetOrdinal("title")),
                reader.GetString(reader.GetOrdinal("source_type")),
                (int)reader.GetInt64(reader.GetOrdinal("chunk_count")),
                reader.GetDateTime(reader.GetOrdinal("uploaded_at"))));
        }
        return results;
    }

    private static List<string> ChunkText(string text, int maxChars, int overlap)
    {
        var chunks = new List<string>();
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        var current = "";
        foreach (var para in paragraphs)
        {
            if ((current + "\n\n" + para).Length <= maxChars)
            {
                current = string.IsNullOrEmpty(current) ? para : current + "\n\n" + para;
            }
            else
            {
                if (!string.IsNullOrEmpty(current)) chunks.Add(current.Trim());

                if (para.Length <= maxChars)
                {
                    current = para;
                }
                else
                {
                    var sentences = para.Split(new[] { ". ", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries);
                    current = "";
                    foreach (var s in sentences)
                    {
                        if ((current + " " + s).Length <= maxChars)
                            current = string.IsNullOrEmpty(current) ? s : current + " " + s;
                        else
                        {
                            if (!string.IsNullOrEmpty(current)) chunks.Add(current.Trim() + ".");
                            current = s;
                        }
                    }
                }
            }
        }
        if (!string.IsNullOrEmpty(current)) chunks.Add(current.Trim());
        return chunks;
    }
}