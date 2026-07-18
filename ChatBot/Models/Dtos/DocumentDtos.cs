namespace ChatBot.Models.Dtos;

/// <summary>Thông tin tổng quan một tài liệu đã index, dùng cho GET /api/documents.</summary>
public class DocumentSummaryDto
{
    public required string Filename { get; set; }

    public int ChunkCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
