using Godot;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base;


namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Defenders;

public partial class CowUnit : DefenderUnit
{
    protected override void OnActionTimerTimeout()
    {
        // Cow doesn't do much actively, maybe moos?
        // GD.Print("Moo!");
    }
}
