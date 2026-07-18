using ChatBot.Models.Dtos;
using ChatBot.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatBot.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IRagService _ragService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IRagService ragService, ILogger<ChatController> logger)
    {
        _ragService = ragService;
        _logger = logger;
    }

    /// <summary>Hỏi đáp RAG: embed câu hỏi -> vector search top-5 -> build prompt -> gọi Claude.</summary>
    [HttpPost("ask")]
    public async Task<ActionResult<AskResponse>> Ask([FromBody] AskRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { error = "Thiếu 'question'." });
        }

        try
        {
            var response = await _ragService.AskAsync(request.Question, ct);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            // Ví dụ: thiếu cấu hình Claude:ApiKey.
            _logger.LogError(ex, "Lỗi cấu hình khi xử lý câu hỏi");
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý câu hỏi: {Question}", request.Question);
            return StatusCode(500, new { error = "Có lỗi xảy ra khi xử lý câu hỏi. Vui lòng thử lại sau." });
        }
    }
}
