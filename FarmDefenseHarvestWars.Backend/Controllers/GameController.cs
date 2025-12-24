using FarmDefenseHarvestWars.Backend.Models;
using FarmDefenseHarvestWars.Shared.Models; // Importăm DTO-ul
using FarmDefenseHarvestWars.Shared.Models.Game;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FarmDefenseHarvestWars.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // <--- ASTA E CHEIA: Nimeni nu intră aici fără token valid!
public class GameController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GameController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<PlayerProfileDto>> GetProfile()
    {
        // 1. Identificăm cine a făcut cererea pe baza Token-ului
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        // 2. Creăm pachetul de date (DTO)
        var profile = new PlayerProfileDto
        {
            Email = user.Email!,
            Gold = user.Gold,
            Level = user.Level,
            Xp = user.Xp
        };

        // 3. Trimitem datele înapoi
        return Ok(profile);
    }
}