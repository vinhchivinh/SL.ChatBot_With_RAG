using ChatBot.Models.Dtos;

namespace ChatBot.Services;

/// <summary>Sinh câu trả lời từ 1 LLM bất kỳ (Claude, QWen3, ...) dựa trên các đoạn ngữ cảnh (RAG).</summary>
public interface ILLMService
{
    Task<string> AskWithContextAsync(string question, IReadOnlyList<SourceDto> contextChunks, CancellationToken ct = default);
}