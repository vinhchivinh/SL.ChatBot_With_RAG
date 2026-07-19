using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChatBot.Services;

/// <summary>
/// Chọn implementation ILLMService (Claude, QWen3, ...) tại runtime dựa vào cấu hình
/// "ProviderLLM:Name" — cho phép đổi nhà cung cấp LLM chỉ bằng đổi appsettings.json /
/// user-secrets, không cần sửa code hay build lại.
/// </summary>
public class FactoryLLMProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _providerName;

    public FactoryLLMProvider(IOptions<LLMProviderOptions> options, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _providerName = options.Value.Name;
    }

    public ILLMService GetLLMService()
    {
        // Chỉ resolve đúng 1 provider được chọn -> provider còn lại không được khởi tạo,
        // nên không cần cấu hình API key của provider không dùng tới (VD chọn QWen3 thì
        // không bắt buộc phải có Claude:ApiKey).
        return _providerName.Trim().ToUpperInvariant() switch
        {
            "QWEN3" => _serviceProvider.GetRequiredService<Qwen3Service>(),
            "CLAUDE" => _serviceProvider.GetRequiredService<ClaudeService>(),
            _ => throw new InvalidOperationException(
                $"ProviderLLM:Name = '{_providerName}' không hợp lệ. Giá trị hỗ trợ: 'Claude', 'QWen3'."),
        };
    }
}
