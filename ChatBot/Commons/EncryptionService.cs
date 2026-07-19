using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace ChatBot.Commons;

/// <summary>
/// Cài đặt IEncryptionService bằng AES-256-GCM — thuật toán "authenticated encryption":
/// ngoài mã hóa nội dung, nó còn sinh ra 1 "tag" xác thực đi kèm để phát hiện dữ liệu có
/// bị chỉnh sửa/giả mạo hay không khi giải mã (khác với AES-CBC thường phải tự ghép thêm
/// HMAC riêng mới có được tính năng này).
/// </summary>
public class EncryptionService : IEncryptionService
{
    private const int NonceSize = 12; // 96-bit — kích thước nonce chuẩn khuyến nghị cho AES-GCM
    private const int TagSize = 16;   // 128-bit authentication tag
    private const int KeySize = 32;   // AES-256 = khóa 32 byte

    private readonly byte[] _key;

    public EncryptionService(IOptions<EncryptionOptions> options)
    {
        var keyBase64 = options.Value.Key;

        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            throw new InvalidOperationException(
                "Chưa cấu hình Encryption:Key. Sinh key mới bằng EncryptionService.GenerateKey(), " +
                "sau đó chạy 'dotnet user-secrets set \"Encryption:Key\" \"<key>\"' trong thư mục project.");
        }

        byte[] key;
        try
        {
            key = Convert.FromBase64String(keyBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Encryption:Key không phải chuỗi Base64 hợp lệ.", ex);
        }

        if (key.Length != KeySize)
        {
            throw new InvalidOperationException(
                $"Encryption:Key phải là {KeySize} byte (AES-256) sau khi decode Base64, hiện tại là {key.Length} byte. " +
                "Sinh key đúng chuẩn bằng EncryptionService.GenerateKey().");
        }

        _key = key;
    }

    public string Encrypt(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        // Nonce PHẢI random cho mỗi lần mã hóa và không bao giờ tái sử dụng với cùng 1 key —
        // tái sử dụng nonce là lỗi bảo mật nghiêm trọng nhất của AES-GCM (lộ thông tin, thậm chí lộ key).
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using (var aesGcm = new AesGcm(_key, TagSize))
        {
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        // Gói nonce + tag + ciphertext thành 1 mảng byte duy nhất để trả về đúng 1 chuỗi
        // (tiện lưu vào 1 cột DB hoặc truyền đi), Decrypt() sẽ tách lại theo đúng thứ tự này.
        var packed = new byte[NonceSize + TagSize + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, packed, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, packed, NonceSize, TagSize);
        Buffer.BlockCopy(cipherBytes, 0, packed, NonceSize + TagSize, cipherBytes.Length);

        return Convert.ToBase64String(packed);
    }

    public string Decrypt(string cipherText)
    {
        ArgumentNullException.ThrowIfNull(cipherText);

        byte[] packed;
        try
        {
            packed = Convert.FromBase64String(cipherText);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Chuỗi mã hóa không hợp lệ (không phải Base64).", ex);
        }

        if (packed.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Chuỗi mã hóa không hợp lệ (thiếu dữ liệu).");
        }

        var nonce = packed.AsSpan(0, NonceSize);
        var tag = packed.AsSpan(NonceSize, TagSize);
        var cipherBytes = packed.AsSpan(NonceSize + TagSize);
        var plainBytes = new byte[cipherBytes.Length];

        using (var aesGcm = new AesGcm(_key, TagSize))
        {
            // Nếu sai key hoặc dữ liệu bị chỉnh sửa (dù chỉ 1 byte), dòng này sẽ throw
            // CryptographicException — đây chính là cơ chế "xác thực" của AES-GCM.
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>Sinh 1 key AES-256 ngẫu nhiên (Base64) — dùng để tạo giá trị cho Encryption:Key.</summary>
    public static string GenerateKey()
    {
        var key = new byte[KeySize];
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }
}
