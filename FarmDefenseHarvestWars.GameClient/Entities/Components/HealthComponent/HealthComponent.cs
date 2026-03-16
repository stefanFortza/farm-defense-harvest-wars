using Godot;
using System;

namespace FarmDefenseHarvestWars.GameClient.Entities.Components;

public partial class HealthComponent : Node
{
    [Signal] public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);
    [Signal] public delegate void DiedEventHandler();

    public int MaxHealth { get; private set; }

    private int _currentHealth;

    // [Export] permite sincronizarea automată a valorii prin MultiplayerSynchronizer
    [Export]
    public int CurrentHealth
    {
        get => _currentHealth;
        private set
        {
            // Evităm procesarea redundantă dacă valoarea nu se schimbă
            if (_currentHealth == value) return;

            int oldHealth = _currentHealth;
            _currentHealth = Math.Clamp(value, 0, MaxHealth);

            // Emitem semnalele pe ORICE instanță (Server sau Client) atunci când valoarea se modifică
            EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);

            if (_currentHealth <= 0 && oldHealth > 0)
            {
                EmitSignal(SignalName.Died);
            }
        }
    }

    public bool IsInitialized { get; private set; } = false;

    public void Initialize(int maxHealth)
    {
        if (IsInitialized) return;

        MaxHealth = maxHealth;
        CurrentHealth = MaxHealth; // Acest apel va declanșa setter-ul și semnalele
        IsInitialized = true;
    }

    public void TakeDamage(int amount)
    {
        // Regula de bază: Doar autoritatea (Serverul) are dreptul să aplice damage
        if (!Multiplayer.IsServer() || CurrentHealth <= 0 || amount <= 0) return;

        CurrentHealth -= amount;
        // Nu mai este nevoie să emitem semnalele aici, setter-ul se ocupă de tot
    }

    public void Heal(int amount)
    {
        if (!Multiplayer.IsServer() || CurrentHealth <= 0 || amount <= 0) return;

        CurrentHealth += amount;
    }

    public void SetMaxHealth(int amount, bool resetHealth = true)
    {
        if (!Multiplayer.IsServer()) return;

        MaxHealth = Math.Max(1, amount); // Prevenim MaxHealth <= 0
        if (resetHealth)
        {
            CurrentHealth = MaxHealth;
        }
    }
}