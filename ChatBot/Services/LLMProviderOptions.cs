namespace ChatBot.Services;

/// <summary>Cấu hình chọn nhà cung cấp LLM (Claude, QWen3, ...) cho FactoryLLMProvider.</summary>
public class LLMProviderOptions
{
    public const string SectionName = "ProviderLLM";

    /// <summary>Tên provider muốn dùng, VD "Claude" hoặc "QWen3". Không phân biệt hoa/thường.</summary>
    public string Name { get; set; } = "Claude";
}
