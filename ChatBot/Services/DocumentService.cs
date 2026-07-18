using ChatBot.Data;
using ChatBot.Models.Dtos;
using ChatBot.Models.Entities;
using Pgvector;

namespace ChatBot.Services;

public class DocumentService : IDocumentService
{
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IDocumentChunkRepository _repository;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IDocumentChunkRepository repository,
        ILogger<DocumentService> logger)
    {
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _repository = repository;
        _logger = logger;
    }

    public async Task<IngestResponse> IngestAsync(string filename, string content, CancellationToken ct = default)
    {
        // Nếu file đã tồn tại, xoá hết chunk cũ trước để re-index (theo yêu cầu: cập nhật tài liệu ERP dễ dàng).
        var deleted = await _repository.DeleteByFilenameAsync(filename, ct);
        if (deleted > 0)
        {
            _logger.LogInformation("Đã xoá {Count} chunk cũ của '{Filename}' trước khi re-index.", deleted, filename);
        }

        // 1) Chunking: chia văn bản dài thành các đoạn nhỏ để mỗi đoạn mang đủ ngữ cảnh
        // nhưng vẫn đủ ngắn để embedding chính xác và không vượt giới hạn token của model.
        var textChunks = _chunkingService.ChunkText(content);

        if (textChunks.Count == 0)
        {
            return new IngestResponse { Filename = filename, ChunksCreated = 0, Status = "empty_content" };
        }

        // 2) Embedding: sinh vector 384 chiều local cho từng chunk.
        var embeddings = await _embeddingService.EmbedManyAsync(textChunks, ct);

        var chunks = textChunks.Select((text, index) => new DocumentChunk
        {
            Id = Guid.NewGuid(),
            Filename = filename,
            ChunkIndex = index,
            Content = text,
            Embedding = new Vector(embeddings[index]),
            CreatedAt = DateTimeOffset.UtcNow,
        }).ToList();

        // 3) Lưu vào pgvector.
        await _repository.InsertChunksAsync(chunks, ct);

        return new IngestResponse { Filename = filename, ChunksCreated = chunks.Count, Status = "indexed" };
    }

    public Task<List<DocumentSummaryDto>> ListDocumentsAsync(CancellationToken ct = default) =>
        _repository.ListDocumentsAsync(ct);

    public async Task<bool> DeleteDocumentAsync(string filename, CancellationToken ct = default)
    {
        var deleted = await _repository.DeleteByFilenameAsync(filename, ct);
        return deleted > 0;
    }
}
