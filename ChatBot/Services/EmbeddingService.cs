using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace ChatBot.Services;

/// <summary>
/// Chạy inference ONNX Runtime cho model all-MiniLM-L6-v2 để sinh embedding local,
/// không phụ thuộc API ngoài. Dùng BertTokenizer (WordPiece) từ Microsoft.ML.Tokenizers
/// để tokenize, sau đó mean-pooling + L2-normalize theo đúng cách sentence-transformers
/// huấn luyện model này.
/// </summary>
public class EmbeddingService : IEmbeddingService, IDisposable
{
    private readonly InferenceSession _session;
    private readonly BertTokenizer _tokenizer;
    private readonly EmbeddingOptions _options;
    private readonly string _inputIdsName;
    private readonly string? _attentionMaskName;
    private readonly string? _tokenTypeIdsName;
    private readonly string _outputName;

    public EmbeddingService(IOptions<EmbeddingOptions> options, IWebHostEnvironment env)
    {
        _options = options.Value;

        var modelPath = ResolvePath(env, _options.ModelPath);
        var vocabPath = ResolvePath(env, _options.VocabPath);

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException(
                $"Không tìm thấy model ONNX tại '{modelPath}'. Xem hướng dẫn tải model all-MiniLM-L6-v2 trong README.",
                modelPath);
        }

        if (!File.Exists(vocabPath))
        {
            throw new FileNotFoundException(
                $"Không tìm thấy vocab.txt tại '{vocabPath}'. Xem hướng dẫn tải model all-MiniLM-L6-v2 trong README.",
                vocabPath);
        }

        _session = new InferenceSession(modelPath);
        _tokenizer = BertTokenizer.Create(vocabPath);

        // Dò tên input/output thực tế của model thay vì hard-code, vì các bản export ONNX
        // khác nhau (transformers.onnx, optimum, ...) có thể đặt tên input hơi khác nhau.
        var inputNames = _session.InputMetadata.Keys.ToList();
        _inputIdsName = inputNames.FirstOrDefault(n => n.Contains("input_ids", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Model ONNX không có input 'input_ids'.");
        _attentionMaskName = inputNames.FirstOrDefault(n => n.Contains("attention_mask", StringComparison.OrdinalIgnoreCase));
        _tokenTypeIdsName = inputNames.FirstOrDefault(n => n.Contains("token_type_ids", StringComparison.OrdinalIgnoreCase));
        _outputName = _session.OutputMetadata.Keys.FirstOrDefault(n => n.Contains("last_hidden_state", StringComparison.OrdinalIgnoreCase))
            ?? _session.OutputMetadata.Keys.First();
    }

    private static string ResolvePath(IWebHostEnvironment env, string configuredPath) =>
        Path.IsPathRooted(configuredPath) ? configuredPath : Path.Combine(env.ContentRootPath, configuredPath);

    public Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(EmbedInternal(text));
    }

    public async Task<List<float[]>> EmbedManyAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        var results = new List<float[]>();
        foreach (var text in texts)
        {
            ct.ThrowIfCancellationRequested();
            results.Add(await EmbedAsync(text, ct));
        }

        return results;
    }

    private float[] EmbedInternal(string text)
    {
        var ids = _tokenizer.EncodeToIds(text, addSpecialTokens: true);
        if (ids.Count > _options.MaxTokens)
        {
            ids = ids.Take(_options.MaxTokens).ToList();
        }

        var seqLen = ids.Count;
        var inputIdsTensor = new DenseTensor<long>(new[] { 1, seqLen });
        var attentionMaskTensor = new DenseTensor<long>(new[] { 1, seqLen });
        var tokenTypeIdsTensor = new DenseTensor<long>(new[] { 1, seqLen });

        for (var i = 0; i < seqLen; i++)
        {
            inputIdsTensor[0, i] = ids[i];
            attentionMaskTensor[0, i] = 1; // không padding vì xử lý từng câu một (batch size 1)
            tokenTypeIdsTensor[0, i] = 0; // chỉ có 1 câu -> token type luôn là 0
        }

        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(_inputIdsName, inputIdsTensor) };
        if (_attentionMaskName is not null)
        {
            inputs.Add(NamedOnnxValue.CreateFromTensor(_attentionMaskName, attentionMaskTensor));
        }

        if (_tokenTypeIdsName is not null)
        {
            inputs.Add(NamedOnnxValue.CreateFromTensor(_tokenTypeIdsName, tokenTypeIdsTensor));
        }

        using var results = _session.Run(inputs);
        var lastHiddenState = results.First(r => r.Name == _outputName).AsTensor<float>();

        return MeanPoolAndNormalize(lastHiddenState, seqLen);
    }

    /// <summary>
    /// Mean pooling: trung bình cộng embedding của tất cả token (loại padding), sau đó
    /// chuẩn hoá L2 (norm = 1) để có thể so sánh bằng cosine similarity một cách chính xác —
    /// đây chính là cách all-MiniLM-L6-v2 được huấn luyện để tạo sentence embedding.
    /// </summary>
    private float[] MeanPoolAndNormalize(Tensor<float> lastHiddenState, int seqLen)
    {
        var hiddenSize = _options.EmbeddingDimension;
        var pooled = new float[hiddenSize];

        for (var t = 0; t < seqLen; t++)
        {
            for (var h = 0; h < hiddenSize; h++)
            {
                pooled[h] += lastHiddenState[0, t, h];
            }
        }

        for (var h = 0; h < hiddenSize; h++)
        {
            pooled[h] /= seqLen;
        }

        var norm = MathF.Sqrt(pooled.Sum(v => v * v));
        if (norm > 1e-9f)
        {
            for (var h = 0; h < hiddenSize; h++)
            {
                pooled[h] /= norm;
            }
        }

        return pooled;
    }

    public void Dispose()
    {
        _session.Dispose();
        GC.SuppressFinalize(this);
    }
}
