using Godot;
using FarmDefenseHarvestWars.Shared.Enums;
namespace FarmDefenseHarvestWars.GameClient.Scripts.Data;

[GlobalClass] // Esențial pentru a crea fișiere .tres în editor
public partial class UnitData : Resource
{
    [ExportGroup("Identity")]
    [Export] public UnitType Type { get; set; }
    [Export] public string Name { get; set; } = "Unit";
    [Export] public Texture2D Icon { get; set; } = null!;

    [ExportGroup("Visuals")]
    [Export] public PackedScene UnitScene { get; set; } = null!;

    [ExportGroup("Stats")]
    [Export] public int Cost { get; set; }
    [Export] public int MaxHealth { get; set; }
    [Export] public int Damage { get; set; }
    [Export] public float AttackRange { get; set; }
    [Export] public float AttackSpeed { get; set; }
}