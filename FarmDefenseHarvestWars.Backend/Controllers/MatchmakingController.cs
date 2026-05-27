using FarmDefenseHarvestWars.Backend.Models;
using FarmDefenseHarvestWars.Backend.Services;
using FarmDefenseHarvestWars.Shared.Enums;
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
    public async Task<ActionResult<MatchmakingStatusDto>> QueueForMatch(
        [FromQuery] PlayerRole preferredRole = PlayerRole.Any,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        try
        {
            var status = await _matchmakingService.QueueForMatchAsync(user.Id, preferredRole, cancellationToken);
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

    // TODO - Add authentication/authorization to this endpoint, ensuring only the match server can call it (e.g., via a shared secret or client certificate)

    [AllowAnonymous]
    [HttpPost("matchmaking/match/{matchId}/complete")]
    public async Task<ActionResult> CompleteMatch(
        string matchId,
        [FromBody] MatchCompletionRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(matchId))
        {
            return BadRequest("Match id is required.");
        }

        if (!Request.Headers.TryGetValue("X-Match-Server-Key", out StringValues providedKey))
        {
            return Unauthorized("Missing match server callback key.");
        }

        try
        {
            await _matchmakingService.CompleteMatchAsync(matchId, providedKey.ToString(), request);

            _logger.LogInformation(
                "Match {MatchId} completion callback accepted. Winner={WinnerRole}, Reason={Reason}, IsAborted={IsAborted}",
                matchId,
                request.WinnerRole,
                request.TerminationReason,
                request.IsAborted);

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpGet("matchmaking/match/{matchId}/reward")]
    public async Task<ActionResult<MatchRewardDto>> GetMatchReward(string matchId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var reward = await _matchmakingService.GetMatchRewardAsync(matchId, userId);
        if (reward == null)
        {
            return NotFound("Match reward not found or not authorized for this user.");
        }

        return Ok(reward);
    }
}
