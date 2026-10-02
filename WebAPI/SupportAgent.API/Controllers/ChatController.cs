using Microsoft.AspNetCore.Mvc;
using SupportAgent.Api.Models;
using SupportAgent.Api.Services;

namespace SupportAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatOrchestratorService _orchestrator;

    public ChatController(ChatOrchestratorService orchestrator) => _orchestrator = orchestrator;

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new ChatResponse("", Guid.NewGuid(), new(), false, "Message cannot be empty."));

        var response = await _orchestrator.RespondAsync(request);
        return Ok(response);
    }
}