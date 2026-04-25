using FarmDefenseHarvestWars.Backend.Models;
using FarmDefenseHarvestWars.Backend.Services;
using FarmDefenseHarvestWars.Shared.Models.Game;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace FarmDefenseHarvestWars.Backend.Controllers;

[ApiController]
[Route("api/game")]
[Authorize]
public class MatchmakingController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMatchmakingService _matchmakingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MatchmakingController> _logger;

    public MatchmakingController(
        UserManager<ApplicationUser> userManager,
        IMatchmakingService matchmakingService,
        IConfiguration configuration,
        ILogger<MatchmakingController> logger)
    {
        _userManager = userManager;
        _matchmakingService = matchmakingService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("matchmaking/queue")]
    public async Task<ActionResult<MatchmakingStatusDto>> QueueForMatch(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        try
        {
            var status = await _matchmakingService.QueueForMatchAsync(user.Id, cancellationToken);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during matchmaking queue for user {UserId}", user.Id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to start match server.");
        }
    }

    [HttpDelete("matchmaking/queue")]
    public ActionResult CancelMatchmaking()
    {
        string? userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User not found.");
        }

        _matchmakingService.CancelMatchmaking(userId);
        return NoContent();
    }

    [HttpGet("matchmaking/status")]
    public ActionResult<MatchmakingStatusDto> GetMatchmakingStatus()
    {
        string? userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User not found.");
        }

        return Ok(_matchmakingService.GetStatusForUser(userId));
    }

    [AllowAnonymous]
    [HttpPost("matchmaking/match/{matchId}/complete")]
    public ActionResult CompleteMatch(
        string matchId,
        [FromBody] MatchCompletionRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(matchId))
        {
            return BadRequest("Match id is required.");
        }

        if (!IsValidMatchServerCallback())
        {
            return Unauthorized("Missing or invalid match server callback key.");
        }

        _matchmakingService.CompleteMatch(matchId);

        _logger.LogInformation(
            "Match {MatchId} completion callback accepted. Winner={WinnerRole}, Reason={Reason}, IsAborted={IsAborted}",
            matchId,
            request.WinnerRole,
            request.TerminationReason,
            request.IsAborted);

        return NoContent();
    }

    private bool IsValidMatchServerCallback()
    {
        string? expectedKey = _configuration["GodotServer:CallbackKey"];
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            return true;
        }

        if (!Request.Headers.TryGetValue("X-Match-Server-Key", out StringValues providedKey))
        {
            return false;
        }

        return string.Equals(expectedKey, providedKey.ToString(), StringComparison.Ordinal);
    }
}
