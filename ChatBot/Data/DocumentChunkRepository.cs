using ChatBot.Models.Dtos;
using ChatBot.Models.Entities;
using Npgsql;
using Pgvector;

namespace ChatBot.Data;

public class DocumentChunkRepository : IDocumentChunkRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public DocumentChunkRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task InsertChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        const string sql = """
            INSERT INTO document_chunks (id, filename, chunk_index, content, embedding, created_at)
            VALUES ($1, $2, $3, $4, $5, $6)
            """;

        foreach (var chunk in chunks)
        {
            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue(chunk.Id);
            cmd.Parameters.AddWithValue(chunk.Filename);
            cmd.Parameters.AddWithValue(chunk.ChunkIndex);
            cmd.Parameters.AddWithValue(chunk.Content);
            cmd.Parameters.AddWithValue(chunk.Embedding);
            cmd.Parameters.AddWithValue(chunk.CreatedAt);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task<List<(DocumentChunk Chunk, double Distance)>> SearchTopKAsync(Vector queryEmbedding, int topK, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, filename, chunk_index, content, embedding, created_at,
                   embedding <=> $1 AS distance
            FROM document_chunks
            ORDER BY embedding <=> $1
            LIMIT $2
            """;

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(queryEmbedding);
        cmd.Parameters.AddWithValue(topK);

        var results = new List<(DocumentChunk, double)>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var chunk = new DocumentChunk
            {
                Id = reader.GetGuid(0),
                Filename = reader.GetString(1),
                ChunkIndex = reader.GetInt32(2),
                Content = reader.GetString(3),
                Embedding = reader.GetFieldValue<Vector>(4),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(5),
            };
            var distance = reader.GetDouble(6);
            results.Add((chunk, distance));
        }

        return results;
    }

    public async Task<List<DocumentSummaryDto>> ListDocumentsAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT filename, COUNT(*) AS chunk_count, MIN(created_at) AS created_at
            FROM document_chunks
            GROUP BY filename
            ORDER BY MIN(created_at) DESC
            """;

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);

        var results = new List<DocumentSummaryDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new DocumentSummaryDto
            {
                Filename = reader.GetString(0),
                ChunkCount = (int)reader.GetInt64(1),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(2),
            });
        }

        return results;
    }

    public async Task<int> DeleteByFilenameAsync(string filename, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM document_chunks WHERE filename = $1";

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(filename);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> DocumentExistsAsync(string filename, CancellationToken ct = default)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM document_chunks WHERE filename = $1)";

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(filename);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is true;
    }
}
