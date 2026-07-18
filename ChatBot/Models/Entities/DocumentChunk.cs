using Pgvector;

namespace ChatBot.Models.Entities;

/// <summary>
/// Một chunk (đoạn nhỏ) của tài liệu đã được embed, lưu trong bảng document_chunks.
/// </summary>
public class DocumentChunk
{
    public Guid Id { get; set; }

    public required string Filename { get; set; }

    public int ChunkIndex { get; set; }

    public required string Content { get; set; }

    /// <summary>Vector 384 chiều sinh ra từ model all-MiniLM-L6-v2.</summary>
    public required Vector Embedding { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
