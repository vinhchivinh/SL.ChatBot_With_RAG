using System.Text;
using Anthropic;
using Anthropic.Models.Messages;
using ChatBot.Models.Dtos;
using Microsoft.Extensions.Options;

namespace ChatBot.Services;

/// <summary>
/// Build prompt RAG (ngữ cảnh + câu hỏi) và gọi Claude API để sinh câu trả lời tiếng Việt.
/// Đây là bước duy nhất gọi ra ngoài (embedding chạy local ở EmbeddingService).
/// </summary>
public class ClaudeService : IClaudeService
{
    private const string SystemPrompt = """
        Bạn là trợ lý ảo hỗ trợ nghiệp vụ cho hệ thống ERP của công ty.
        Chỉ trả lời dựa trên NGỮ CẢNH được cung cấp bên dưới, không tự suy diễn hay bịa thông tin ngoài ngữ cảnh.
        Nếu ngữ cảnh không đủ để trả lời, hãy nói rõ là chưa tìm thấy thông tin liên quan trong tài liệu và đề nghị người dùng liên hệ bộ phận phù hợp.
        Trả lời ngắn gọn, chính xác, bằng tiếng Việt, đúng trọng tâm câu hỏi.
        """;

    private readonly AnthropicClient _client;
    private readonly ClaudeOptions _options;

    public ClaudeService(IOptions<ClaudeOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "Chưa cấu hình Claude:ApiKey. Chạy 'dotnet user-secrets set \"Claude:ApiKey\" \"<key>\"' trong thư mục project.");
        }

        _client = new AnthropicClient { ApiKey = _options.ApiKey };
    }

    public async Task<string> AskWithContextAsync(string question, IReadOnlyList<SourceDto> contextChunks, CancellationToken ct = default)
    {
        var userMessage = BuildUserMessage(question, contextChunks);

        var parameters = new MessageCreateParams
        {
            Model = Model.ClaudeHaiku4_5,
            MaxTokens = _options.MaxTokens,
            System = SystemPrompt,
            Messages = [new() { Role = Role.User, Content = userMessage }],
        };

        var response = await _client.Messages.Create(parameters, cancellationToken: ct);

        var textBlock = response.Content.Select(b => b.Value).OfType<TextBlock>().FirstOrDefault();
        return textBlock?.Text ?? string.Empty;
    }

    private static string BuildUserMessage(string question, IReadOnlyList<SourceDto> contextChunks)
    {
        var sb = new StringBuilder();

        if (contextChunks.Count == 0)
        {
            sb.AppendLine("Không có tài liệu nào liên quan được tìm thấy trong kho dữ liệu.");
        }
        else
        {
            sb.AppendLine("Ngữ cảnh:");
            for (var i = 0; i < contextChunks.Count; i++)
            {
                var c = contextChunks[i];
                sb.AppendLine($"[{i + 1}] (nguồn: {c.Filename})");
                sb.AppendLine(c.Content);
                sb.AppendLine();
            }
        }

        sb.AppendLine($"Câu hỏi: {question}");

        return sb.ToString();
    }
}
