using ChatBot.Models.Dtos;

namespace ChatBot.Services;

/// <summary>Nghiệp vụ quản lý tài liệu: ingest (chunk + embed + lưu), liệt kê, xoá.</summary>
public interface IDocumentService
{
    Task<IngestResponse> IngestAsync(string filename, string content, CancellationToken ct = default);

    Task<List<DocumentSummaryDto>> ListDocumentsAsync(CancellationToken ct = default);

    /// <summary>Trả về false nếu filename không tồn tại (không có chunk nào).</summary>
    Task<bool> DeleteDocumentAsync(string filename, CancellationToken ct = default);
}
