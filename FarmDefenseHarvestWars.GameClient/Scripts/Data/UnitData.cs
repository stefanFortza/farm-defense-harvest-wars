using Godot;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;
using System.Text.Json;
using System.IO;
using System;
namespace FarmDefenseHarvestWars.GameClient.Scripts.Data;

[GlobalClass]
[Tool]
public partial class UnitData : Resource
{
    [ExportGroup("Identity")]
    [Export] public UnitType Type { get; set; }
    [Export] public string Name { get; set; } = "Unit";
    [Export] public Texture2D Icon { get; set; }

    [ExportGroup("In-Game Economy (Match)")]
    [Export] public int MatchCost { get; set; } = 25;

    [ExportGroup("Meta Economy (Backend Shop)")]
    [Export] public int UnlockCost { get; set; } = 100;
    [Export] public bool IsDefaultUnlocked { get; set; } = true;

    [ExportGroup("Visuals")]
    [Export(PropertyHint.File, "*.tscn")] public string UnitScenePath { get; set; } = string.Empty;
    [Export] public PackedScene ProjectileScene { get; set; } = null!;

    [ExportGroup("Stats")]
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public int Damage { get; set; } = 10;
    [Export] public float Speed { get; set; } = 10.0f;
    [Export] public float AttackRange { get; set; } = 64f;
    [Export] public float AttackSpeed { get; set; } = 1.0f;

    public UnitDataDto ToDto()
    {
        return new UnitDataDto
        {
            Type = Type,
            Name = Name,
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

