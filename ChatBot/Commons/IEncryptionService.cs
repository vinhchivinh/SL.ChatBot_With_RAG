namespace ChatBot.Commons;

/// <summary>
/// Mã hóa/giải mã chuỗi dùng chung (AES-256-GCM). Dùng cho các nhu cầu chưa gắn cố định
/// vào một chỗ cụ thể — ví dụ mã hóa dữ liệu nhạy cảm trước khi lưu DB, mã hóa payload
/// trao đổi với hệ thống khác, v.v. Gọi ở đâu cần thì inject IEncryptionService vào đó.
/// </summary>
public interface IEncryptionService
{
    /// <summary>Mã hóa plainText, trả về 1 chuỗi Base64 (chứa nonce + tag + ciphertext).</summary>
    string Encrypt(string plainText);

    /// <summary>Giải mã chuỗi do Encrypt() sinh ra. Throw CryptographicException nếu sai key hoặc dữ liệu bị chỉnh sửa.</summary>
    string Decrypt(string cipherText);
}
