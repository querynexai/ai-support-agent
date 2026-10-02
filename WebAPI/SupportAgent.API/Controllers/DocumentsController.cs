using Microsoft.AspNetCore.Mvc;
using SupportAgent.Api.Models;
using SupportAgent.Api.Services;

namespace SupportAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentIngestionService _ingestion;

    public DocumentsController(DocumentIngestionService ingestion) => _ingestion = ingestion;

    [HttpGet]
    public Task<List<DocumentResponse>> List() => _ingestion.ListDocumentsAsync();

    [HttpPost]
    public async Task<ActionResult<IngestResult>> Upload([FromBody] DocumentUploadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new IngestResult(0, 0, false, "Title and content are required."));

        var result = await _ingestion.IngestAsync(request);
        return result.Success ? Ok(result) : StatusCode(500, result);
    }
}