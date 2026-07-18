namespace ChatBot.Services;

/// <summary>
/// Chia văn bản thành các chunk ~1000 ký tự, overlap 200 ký tự.
/// Ưu tiên cắt tại ranh giới câu (. ! ? xuống dòng) gần vị trí mục tiêu nhất để không cắt ngang câu,
/// giúp mỗi chunk vẫn giữ được ngữ cảnh trọn vẹn khi embed.
/// </summary>
public class ChunkingService : IChunkingService
{
    private const int TargetChunkSize = 1000;
    private const int Overlap = 200;
    private static readonly char[] SentenceBoundaries = ['.', '!', '?', '\n'];

    public List<string> ChunkText(string text)
    {
        var chunks = new List<string>();
        text = text.Trim();

        if (string.IsNullOrEmpty(text))
        {
            return chunks;
        }

        if (text.Length <= TargetChunkSize)
        {
            chunks.Add(text);
            return chunks;
        }

        var start = 0;
        while (start < text.Length)
        {
            var remaining = text.Length - start;
            if (remaining <= TargetChunkSize)
            {
                chunks.Add(text[start..].Trim());
                break;
            }

            var end = FindCutPoint(text, start, start + TargetChunkSize);
            chunks.Add(text[start..end].Trim());

            // Chunk tiếp theo lùi lại "Overlap" ký tự để giữ ngữ cảnh, nhưng luôn tiến về phía trước.
            var nextStart = end - Overlap;
            start = nextStart > start ? nextStart : end;
        }

        return chunks.Where(c => c.Length > 0).ToList();
    }

    /// <summary>Tìm điểm cắt gần "idealEnd" nhất trong khoảng cho phép, ưu tiên ranh giới câu/đoạn.</summary>
    private static int FindCutPoint(string text, int start, int idealEnd)
    {
        var searchWindowStart = Math.Max(start + TargetChunkSize / 2, idealEnd - 200);
        var searchWindowEnd = Math.Min(text.Length, idealEnd + 1);

        for (var i = searchWindowEnd - 1; i >= searchWindowStart; i--)
        {
            if (SentenceBoundaries.Contains(text[i]))
            {
                return i + 1;
            }
        }

        // Không tìm thấy ranh giới câu phù hợp — cắt cứng tại idealEnd.
        return Math.Min(idealEnd, text.Length);
    }
}
