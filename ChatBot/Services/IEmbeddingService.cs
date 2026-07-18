namespace ChatBot.Services;

/// <summary>
/// Sinh vector embedding 384 chiều bằng model all-MiniLM-L6-v2 chạy LOCAL qua ONNX Runtime.
/// Không gọi API bên ngoài cho bước embed.
/// </summary>
public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);

    Task<List<float[]>> EmbedManyAsync(IEnumerable<string> texts, CancellationToken ct = default);
}
