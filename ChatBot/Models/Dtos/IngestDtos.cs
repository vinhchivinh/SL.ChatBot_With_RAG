namespace ChatBot.Models.Dtos;

/// <summary>Body dùng khi ingest bằng raw text (Content-Type: application/json).</summary>
public class IngestTextRequest
{
    public required string Filename { get; set; }

    public required string Content { get; set; }
}

/// <summary>Kết quả trả về sau khi ingest xong một tài liệu.</summary>
public class IngestResponse
{
    public required string Filename { get; set; }

    public int ChunksCreated { get; set; }

    public required string Status { get; set; }
}
