using ChatBot.Models.Dtos;

namespace ChatBot.Services;

/// <summary>Gọi Claude API (Anthropic) để sinh câu trả lời dựa trên các đoạn ngữ cảnh (RAG).</summary>
public interface IClaudeService
{
    Task<string> AskWithContextAsync(string question, IReadOnlyList<SourceDto> contextChunks, CancellationToken ct = default);
}
