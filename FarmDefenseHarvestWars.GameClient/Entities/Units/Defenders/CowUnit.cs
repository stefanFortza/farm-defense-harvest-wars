using Godot;
using FarmDefenseHarvestWars.Shared.Enums;

public partial class CowUnit : DefenderUnit
{
    protected override void OnActionTimerTimeout()
    {
        // Cow doesn't do much actively, maybe moos?
        // GD.Print("Moo!");
    }
}
