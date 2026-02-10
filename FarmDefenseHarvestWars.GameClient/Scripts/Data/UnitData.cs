using Godot;
using FarmDefenseHarvestWars.Shared.Enums;

[GlobalClass]
public partial class UnitData : Resource
{
    [Export] public UnitType Type;
    [Export] public int Cost;
    [Export] public int MaxHealth;
    [Export] public int Damage;
    [Export] public float AttackRange;
    [Export] public float AttackSpeed;
}
