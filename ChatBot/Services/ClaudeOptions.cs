namespace ChatBot.Services;

/// <summary>Cấu hình gọi Claude API. ApiKey nên đặt qua user-secrets, KHÔNG hardcode.</summary>
public class ClaudeOptions
{
    public const string SectionName = "Claude";

    public string ApiKey { get; set; } = string.Empty;

    public int MaxTokens { get; set; } = 2048;
}
