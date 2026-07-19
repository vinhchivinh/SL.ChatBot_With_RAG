using ChatBot.Models.Dtos;

namespace ChatBot.Services;

/// <summary>
/// TODO: chưa cài đặt — cần xác định endpoint gọi QWen3 (Alibaba DashScope cloud API hay
/// self-hosted qua Ollama/vLLM OpenAI-compatible) và cách xác thực trước khi implement.
/// Được FactoryLLMProvider chọn khi ProviderLLM:Name = "QWen3".
/// </summary>
public class Qwen3Service : ILLMService
{
    public Task<string> AskWithContextAsync(string question, IReadOnlyList<SourceDto> contextChunks, CancellationToken ct = default)
    {
        throw new NotImplementedException(
            "Qwen3Service chưa được cài đặt. Đổi ProviderLLM:Name thành \"Claude\" trong appsettings.json/user-secrets để dùng Claude, hoặc yêu cầu implement Qwen3Service.");
    }
}