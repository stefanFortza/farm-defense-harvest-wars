using Godot;
using Godot.Collections;
using FarmDefenseHarvestWars.Shared.Enums;

namespace FarmDefenseHarvestWars.GameClient.Scripts.Data;

[GlobalClass]
public partial class SelectedDeckData : Resource
{
    [Export] public Array<UnitType> DefenderDeck { get; set; } = [];
    [Export] public Array<UnitType> AttackerDeck { get; set; } = [];
}
