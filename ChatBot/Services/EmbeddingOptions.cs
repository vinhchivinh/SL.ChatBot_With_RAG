namespace ChatBot.Services;

/// <summary>Cấu hình đường dẫn model ONNX + vocab cho embedding local.</summary>
public class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    /// <summary>Đường dẫn tới model.onnx (all-MiniLM-L6-v2), tương đối so với content root.</summary>
    public string ModelPath { get; set; } = "Models/onnx/model.onnx";

    /// <summary>Đường dẫn tới vocab.txt đi kèm model.</summary>
    public string VocabPath { get; set; } = "Models/onnx/vocab.txt";

    /// <summary>Số chiều của vector embedding (all-MiniLM-L6-v2 = 384).</summary>
    public int EmbeddingDimension { get; set; } = 384;

    /// <summary>Số token tối đa mỗi lần encode (model gốc giới hạn 256).</summary>
    public int MaxTokens { get; set; } = 256;
}
