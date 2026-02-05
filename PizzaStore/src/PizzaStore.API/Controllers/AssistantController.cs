using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaStore.API.Assistant;

namespace PizzaStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssistantController : ControllerBase
{
    private readonly IAssistantService _assistantService;

    public AssistantController(IAssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    /// <summary>
    /// Chat with the PizzaStore assistant
    /// </summary>
    /// <param name="request">User message with optional conversation history</param>
    /// <returns>Assistant response message</returns>
    /// <response code="200">Returns the assistant response</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(AssistantChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Chat([FromBody] AssistantChatRequest request, CancellationToken cancellationToken)
    {
        var response = await _assistantService.ChatAsync(request, cancellationToken);
        return Ok(response);
    }
}
