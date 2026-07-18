using ChatBot.Models.Dtos;
using ChatBot.Models.Entities;
using Pgvector;

namespace ChatBot.Data;

/// <summary>
/// Truy cập bảng document_chunks trong PostgreSQL (pgvector).
/// </summary>
public interface IDocumentChunkRepository
{
    Task InsertChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken ct = default);

    /// <summary>Tìm top-K chunk gần nhất với queryEmbedding theo cosine distance.</summary>
    Task<List<(DocumentChunk Chunk, double Distance)>> SearchTopKAsync(Vector queryEmbedding, int topK, CancellationToken ct = default);

    Task<List<DocumentSummaryDto>> ListDocumentsAsync(CancellationToken ct = default);

    /// <summary>Xoá toàn bộ chunk của một filename. Trả về số dòng đã xoá.</summary>
    Task<int> DeleteByFilenameAsync(string filename, CancellationToken ct = default);

    Task<bool> DocumentExistsAsync(string filename, CancellationToken ct = default);
}
