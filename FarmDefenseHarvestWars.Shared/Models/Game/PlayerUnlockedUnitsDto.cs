using FarmDefenseHarvestWars.Shared.Enums;
using System.Collections.Generic;

namespace FarmDefenseHarvestWars.Shared.Models.Game;

public class PlayerUnlockedUnitsDto
{
    public IReadOnlyList<UnitUnlockDto> DefenderUnits { get; set; } = [];
    public IReadOnlyList<UnitUnlockDto> AttackerUnits { get; set; } = [];
}
