using ChatBot.Data;
using ChatBot.Models.Dtos;
using Pgvector;

namespace ChatBot.Services;

public class RagService : IRagService
{
    private const int TopK = 5;

    private readonly IEmbeddingService _embeddingService;
    private readonly IDocumentChunkRepository _repository;
    private readonly ILLMService _iLLMService;
    private readonly ILogger<RagService> _logger;

    public RagService(
        IEmbeddingService embeddingService,
        IDocumentChunkRepository repository,
        ILLMService claudeService,
        ILogger<RagService> logger)
    {
        _embeddingService = embeddingService;
        _repository = repository;
        _iLLMService = claudeService;
        _logger = logger;
    }

    public async Task<AskResponse> AskAsync(string question, CancellationToken ct = default)
    {
       
        var questionEmbedding = await _embeddingService.EmbedAsync(question, ct);

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
        var answer = await _iLLMService.AskWithContextAsync(question, sources, ct);

        return new AskResponse { Answer = answer, Sources = sources };
    }
}
