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
public class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IProfileService _profileService;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        IProfileService profileService)
    {
        _userManager = userManager;
        _profileService = profileService;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<PlayerProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        var profile = await _profileService.GetProfileAsync(user, cancellationToken);
        return Ok(profile);
    }

    [HttpPost("unit/{unitType}/unlock")]
    public async Task<ActionResult<PlayerProfileDto>> UnlockUnit(UnitType unitType, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        try
        {
            PlayerProfileDto profile = await _profileService.UnlockUnitAsync(user, unitType, cancellationToken);
            return Ok(profile);
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

    [HttpPost("avatar/{avatarIndex}")]
    public async Task<ActionResult<PlayerProfileDto>> UpdateAvatar(int avatarIndex, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        try
        {
            PlayerProfileDto profile = await _profileService.UpdateAvatarAsync(user, avatarIndex, cancellationToken);
            return Ok(profile);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("chest/{chestId}/open")]
    public async Task<ActionResult<ChestOpenResultDto>> OpenChest(string chestId, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        try
        {
            var result = await _profileService.OpenChestAsync(user, chestId, cancellationToken);
            return Ok(new ChestOpenResultDto { Profile = result.Profile, Rewards = result.Rewards });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("chest/{chestId}/start-unlock")]
    public async Task<ActionResult<PlayerProfileDto>> StartUnlockChest(string chestId, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        try
        {
            var profile = await _profileService.StartUnlockChestAsync(user, chestId, cancellationToken);
            return Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("unit/{unitType}/upgrade")]
    public async Task<ActionResult<PlayerProfileDto>> UpgradeUnit(UnitType unitType, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        try
        {
            var profile = await _profileService.UpgradeUnitAsync(user, unitType, cancellationToken);
            return Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
