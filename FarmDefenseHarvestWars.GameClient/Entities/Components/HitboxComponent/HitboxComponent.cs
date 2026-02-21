using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using Godot;
using System;

namespace FarmDefenseHarvestWars.GameClient.Entities.Components;

public partial class HitboxComponent : Area2D, IInitializable<int>
{
    public int DamageAmount { get; set; }

    public bool IsInitialized { get; private set; } = false;

    public void Initialize(int damage)
    {
        DamageAmount = damage;
        IsInitialized = true;
    }

    public override void _Ready()
    {
        AreaEntered += OnAreaEntered;
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area is HurtboxComponent hurtbox)
        {
            hurtbox.ReceiveHit(DamageAmount);
        }
    }
}
