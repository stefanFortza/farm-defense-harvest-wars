using Godot;
using System;

public abstract partial class BaseUnit : CharacterBody2D
{
    [Export] public int MaxHealth { get; set; } = 100;

    // Synced via MultiplayerSynchronizer in the future
    public int CurrentHealth { get; protected set; }

    public abstract string UnitName { get; }

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        AddToGroup("Units");
    }

    // Server-side logic
    public virtual void TakeDamage(int amount)
    {
        // In a real scenario, check if IsMultiplayerAuthority()
        CurrentHealth -= amount;
        GD.Print($"{UnitName} took {amount} damage. HP: {CurrentHealth}/{MaxHealth}");

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        GD.Print($"{UnitName} died!");
        QueueFree();
    }
}
