using ChatBot.Models.Dtos;

namespace ChatBot.Services;

/// <summary>Điều phối toàn bộ luồng RAG: embed câu hỏi -> vector search -> gọi Claude -> trả lời + nguồn.</summary>
public interface IRagService
{
    Task<AskResponse> AskAsync(string question, CancellationToken ct = default);
}
