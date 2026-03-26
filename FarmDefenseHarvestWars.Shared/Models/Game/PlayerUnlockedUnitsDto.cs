using FarmDefenseHarvestWars.Shared.Enums;
using System.Collections.Generic;

namespace FarmDefenseHarvestWars.Shared.Models.Game;

public class PlayerUnlockedUnitsDto
{
    public IReadOnlyList<UnitType> DefenderUnits { get; set; } = [];
    public IReadOnlyList<UnitType> AttackerUnits { get; set; } = [];
}
