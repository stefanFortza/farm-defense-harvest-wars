using Godot;
using System;

namespace FarmDefenseHarvestWars.GameClient.Entities.Components;

public partial class HitboxComponent : Area2D
{
    [Export] public int DamageAmount { get; set; } = 10;

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
