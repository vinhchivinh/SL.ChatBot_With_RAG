using ChatBot.Data;
using ChatBot.Models.Dtos;
using Pgvector;

namespace ChatBot.Services;

public class RagService : IRagService
{
    private const int TopK = 5;

    private readonly IEmbeddingService _embeddingService;
    private readonly IDocumentChunkRepository _repository;
    private readonly IClaudeService _claudeService;
    private readonly ILogger<RagService> _logger;

    public RagService(
        IEmbeddingService embeddingService,
        IDocumentChunkRepository repository,
        IClaudeService claudeService,
        ILogger<RagService> logger)
    {
        _embeddingService = embeddingService;
        _repository = repository;
        _claudeService = claudeService;
        _logger = logger;
    }

    public async Task<AskResponse> AskAsync(string question, CancellationToken ct = default)
    {
        // 1) Embed câu hỏi bằng model local (ONNX) — giải thích: "embedding" là biến câu hỏi
        // thành 1 vector số thực để có thể so sánh mức độ liên quan với các chunk đã lưu.
        var questionEmbedding = await _embeddingService.EmbedAsync(question, ct);

        // 2) Vector search: tìm top-5 chunk có "cosine similarity" (độ tương đồng góc giữa 2 vector,
        // giá trị 1 = giống hệt nhau về ngữ nghĩa, 0 = không liên quan) cao nhất với câu hỏi.
        var searchResults = await _repository.SearchTopKAsync(new Vector(questionEmbedding), TopK, ct);

        var sources = searchResults
            .Select(r => new SourceDto
            {
                Filename = r.Chunk.Filename,
                Content = r.Chunk.Content,
                Score = Math.Round(1 - r.Distance, 4),
            })
            .ToList();

        _logger.LogInformation("Tìm thấy {Count} nguồn liên quan cho câu hỏi.", sources.Count);

        // 3) Build prompt (context + question) và gọi Claude để sinh câu trả lời cuối cùng.
        var answer = await _claudeService.AskWithContextAsync(question, sources, ct);

        return new AskResponse { Answer = answer, Sources = sources };
    }
}
