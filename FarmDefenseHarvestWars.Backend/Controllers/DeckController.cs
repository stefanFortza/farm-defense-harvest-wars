using FarmDefenseHarvestWars.Backend.Models;
using FarmDefenseHarvestWars.Backend.Services;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FarmDefenseHarvestWars.Backend.Controllers;

[ApiController]
[Route("api/game")]
[Authorize]
public class DeckController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDeckService _deckService;

    public DeckController(
        UserManager<ApplicationUser> userManager,
        IDeckService deckService)
    {
        _userManager = userManager;
        _deckService = deckService;
    }

    [HttpGet("deck/{role}")]
    public async Task<ActionResult<DeckDto>> GetDeck(PlayerRole role, CancellationToken cancellationToken)
    {
        if (role is not (PlayerRole.Defender or PlayerRole.Attacker))
        {
            return BadRequest("Role must be Defender or Attacker.");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        var deck = await _deckService.GetDeckAsync(user.Id, role, cancellationToken);
        return Ok(deck);
    }

    [HttpGet("deck/{role}/default")]
    public ActionResult<DeckDto> GetDefaultDeck(PlayerRole role)
    {
        if (role is not (PlayerRole.Defender or PlayerRole.Attacker))
        {
            return BadRequest("Role must be Defender or Attacker.");
        }

        return Ok(_deckService.GetDefaultDeck(role));
    }

    [HttpPut("deck/{role}")]
    public async Task<ActionResult<DeckDto>> UpdateDeck(
        PlayerRole role,
        [FromBody] UpdateDeckDto request,
        CancellationToken cancellationToken)
    {
        if (role is not (PlayerRole.Defender or PlayerRole.Attacker))
        {
            return BadRequest("Role must be Defender or Attacker.");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        try
        {
            var deck = await _deckService.UpdateDeckAsync(user.Id, role, request, cancellationToken);
            return Ok(deck);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
