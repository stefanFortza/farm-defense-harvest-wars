using Godot;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;
namespace FarmDefenseHarvestWars.GameClient.Scripts.Data;

[GlobalClass]
[Tool]
public partial class UnitData : Resource
{
    [ExportGroup("Identity")]
    [Export] public UnitType Type { get; set; }

    [Export] public PlayerRole Role { get; set; }
    [Export] public string Name { get; set; } = "Unit";
    [Export] public Texture2D Icon { get; set; }

    [ExportGroup("In-Game Economy (Match)")]
    [Export] public int MatchCost { get; set; } = 25;
    [Export] public float CardCooldownSeconds { get; set; } = 3.0f;

    [ExportGroup("Meta Economy (Backend Shop)")]
    [Export] public int UnlockCost { get; set; } = 100;
    [Export] public bool IsDefaultUnlocked { get; set; } = true;

    [ExportGroup("Visuals")]
    [Export(PropertyHint.File, "*.tscn")] public string UnitScenePath { get; set; } = string.Empty;
    [Export] public PackedScene ProjectileScene { get; set; } = null!;

    [ExportGroup("Stats")]
    public bool IsStatic => Speed == 0f;
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public int Damage { get; set; } = 10;
    [Export] public float Speed { get; set; } = 0;
    [Export] public float AttackRange { get; set; } = 64f;
    [Export] public float OptimalRange { get; set; } = 56f;
    [Export] public float MeleeRange { get; set; } = 24f;
    [Export] public float AttackSpeed { get; set; } = 0.4f;

    public UnitDataDto ToDto()
    {
        return new UnitDataDto
        {
            Type = Type,
            Name = Name,
            Role = Role,
            MatchCost = MatchCost,
            UnlockCost = UnlockCost,
            IsDefaultUnlocked = IsDefaultUnlocked,
            MaxHealth = MaxHealth,
            Damage = Damage,
            Speed = Speed,
            AttackRange = AttackRange,
            AttackSpeed = AttackSpeed
        };
    }
}

