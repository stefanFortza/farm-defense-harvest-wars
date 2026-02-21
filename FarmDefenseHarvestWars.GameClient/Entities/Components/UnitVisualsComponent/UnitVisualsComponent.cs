using System.ComponentModel;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Entities.Components;

public partial class UnitVisualsComponent : Node
{
    [Export] private ProgressBar HealthBar { get; set; } = null!;
    [Export] private BaseUnit _unit = null!;

    public override void _Ready()
    {
        this.EnsureNotNull(_unit, nameof(_unit));
        this.EnsureNotNull(HealthBar, nameof(HealthBar));

        // Prefer listening to HealthComponent directly
        if (_unit.HealthComponent != null)
        {
            _unit.HealthComponent.HealthChanged += OnHealthChanged;
            // Initialize bar
            OnHealthChanged(_unit.HealthComponent.CurrentHealth, _unit.HealthComponent.MaxHealth);
        }
        else
        {
            // Fallback to BaseUnit signals
            _unit.HealthChanged += OnHealthChanged;
            OnHealthChanged(_unit.MaxHealth, _unit.MaxHealth);
        }
    }

    private void OnHealthChanged(int newHealth, int maxHealth)
    {
        HealthBar.MaxValue = maxHealth;
        HealthBar.Value = newHealth;
        HealthBar.Visible = newHealth < maxHealth; // Hide if full
    }
}
