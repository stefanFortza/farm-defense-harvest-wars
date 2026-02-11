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

    [ExportGroup("In-Game Economy (Match)")]
    [Export] public int MatchCost { get; set; } = 25;

    [ExportGroup("Meta Economy (Backend Shop)")]
    [Export] public int UnlockCost { get; set; } = 100; // Pietre prețioase/Bani reali
    [Export] public bool IsDefaultUnlocked { get; set; } = true; // Unitățile de bază

    [ExportGroup("Visuals")]
    [Export] public PackedScene UnitScene { get; set; } = null!;

    [ExportGroup("Stats")]
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public int Damage { get; set; } = 10;
    [Export] public float AttackRange { get; set; } = 64f;
    [Export] public float AttackSpeed { get; set; } = 1.0f;
}