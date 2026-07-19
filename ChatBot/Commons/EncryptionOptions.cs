namespace ChatBot.Commons;

/// <summary>Cấu hình key cho EncryptionService. Key nên đặt qua user-secrets, KHÔNG hardcode.</summary>
public class EncryptionOptions
{
    public const string SectionName = "Encryption";

    /// <summary>Khóa AES-256 dạng Base64 (32 byte sau khi decode). Sinh bằng EncryptionService.GenerateKey().</summary>
    public string Key { get; set; } = string.Empty;
}
