using Godot;
using FarmDefenseHarvestWars.Shared.Enums;

public partial class CowUnit : DefenderUnit
{
    public override UnitType Type => UnitType.Cow;

    public override void _Ready()
    {
        base._Ready();
        MaxHealth = 500; // High HP for Tank
        CurrentHealth = MaxHealth;
    }

    protected override void OnActionTimerTimeout()
    {
        // Cow doesn't do much actively, maybe moos?
        // GD.Print("Moo!");
    }
}
