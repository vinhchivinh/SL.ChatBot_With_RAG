using System.Text;
using Anthropic.Models.Messages;
using ChatBot.Models.Dtos;
using ChatBot.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatBot.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>Ingest bằng file upload (.txt), multipart/form-data. Field: "file" (bắt buộc), "filename" (tuỳ chọn).</summary>
    [HttpPost("ingest")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)] // 20MB
    public async Task<ActionResult<IngestResponse>> IngestFile(
        IFormFile file,
        [FromForm] string? filename,
        CancellationToken ct) 
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = Commons.Message.MSG_ERROR_FILE_INVALID});
        }

        string content;
        try
        {
            // UTF-8 để đọc đúng tiếng Việt có dấu.
            using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
            content = await reader.ReadToEndAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không đọc được nội dung file {Filename}", file.FileName);
            return BadRequest(new { error = "Không đọc được nội dung file. Hãy đảm bảo file là .txt mã hoá UTF-8." });
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return BadRequest(new { error = "Nội dung file rỗng." });
        }

        var resolvedFilename = string.IsNullOrWhiteSpace(filename) ? file.FileName : filename;

        try
        {
            var result = await _documentService.IngestAsync(resolvedFilename, content, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi ingest file {Filename}", resolvedFilename);
            return StatusCode(500, new { error = "Có lỗi xảy ra khi xử lý tài liệu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>Ingest bằng raw text trong body JSON: { "filename": "...", "content": "..." }.</summary>
    [HttpPost("ingest")]
    [Consumes("application/json")]
    public async Task<ActionResult<IngestResponse>> IngestText([FromBody] IngestTextRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Filename))
        {
            return BadRequest(new { error = "Thiếu 'filename'." });
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { error = "Thiếu 'content'." });
        }

        try
        {
            var result = await _documentService.IngestAsync(request.Filename, request.Content, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi ingest text cho {Filename}", request.Filename);
            return StatusCode(500, new { error = "Có lỗi xảy ra khi xử lý tài liệu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>Danh sách tài liệu đã index: filename, số chunks, ngày tạo.</summary>
    [HttpGet]
    public async Task<ActionResult<List<DocumentSummaryDto>>> GetDocuments(CancellationToken ct)
    {
        try
        {
            var documents = await _documentService.ListDocumentsAsync(ct);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách tài liệu");
            return StatusCode(500, new { error = "Có lỗi xảy ra khi lấy danh sách tài liệu." });
        }
    }

    /// <summary>Xoá tất cả chunk của một file (dùng để re-index khi tài liệu ERP cập nhật).</summary>
    [HttpDelete("{filename}")]
    public async Task<IActionResult> DeleteDocument(string filename, CancellationToken ct)
    {
        try
        {
            var existed = await _documentService.DeleteDocumentAsync(filename, ct);
            if (!existed)
            {
                return NotFound(new { error = $"Không tìm thấy tài liệu '{filename}'." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xoá tài liệu {Filename}", filename);
            return StatusCode(500, new { error = "Có lỗi xảy ra khi xoá tài liệu." });
        }
    }
}
