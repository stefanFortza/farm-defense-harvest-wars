using Godot;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base;

public partial class WolfUnit : AttackerUnit
{
    public override UnitType Type => UnitType.Wolf;

    public override void _Ready()
    {
        base._Ready();
        MaxHealth = 100;
        CurrentHealth = MaxHealth;
        Speed = 150.0f;
        Damage = 15;
    }
}
