namespace ChatBot.Models.Dtos;

public class AskRequest
{
    public required string Question { get; set; }
}

/// <summary>Một nguồn (chunk) được dùng để trả lời câu hỏi, kèm điểm tương đồng.</summary>
public class SourceDto
{
    public required string Filename { get; set; }

    public required string Content { get; set; }

    /// <summary>Cosine similarity: 1 - cosine distance. Càng gần 1 càng liên quan.</summary>
    public double Score { get; set; }
}

public class AskResponse
{
    public required string Answer { get; set; }

    public required List<SourceDto> Sources { get; set; }
}
