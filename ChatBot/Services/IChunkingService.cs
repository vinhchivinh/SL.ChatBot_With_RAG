namespace ChatBot.Services;

public interface IChunkingService
{
    /// <summary>Chia văn bản dài thành các đoạn nhỏ (chunk) để embed và lưu vector.</summary>
    List<string> ChunkText(string text);
}
