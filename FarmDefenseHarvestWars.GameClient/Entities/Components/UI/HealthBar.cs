using Godot;
using System;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;

namespace FarmDefenseHarvestWars.GameClient.Entities.Components.UI;

public partial class HealthBar : Control, IInitializable<HealthComponent>
{
    [Export] private ProgressBar _progressBar = null!;
    
    public bool IsInitialized { get; private set; } = false;

    private HealthComponent _healthComponent = null!;

    public void Initialize(HealthComponent healthComponent)
    {
        if (IsInitialized) return;
        
        _healthComponent = healthComponent;
        _healthComponent.HealthChanged += OnHealthChanged;
        
        // Set initial values
        UpdateDisplay(_healthComponent.CurrentHealth, _healthComponent.MaxHealth);
        
        IsInitialized = true;
    }

    public override void _ExitTree()
    {
        if (_healthComponent != null)
        {
            _healthComponent.HealthChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        UpdateDisplay(currentHealth, maxHealth);
    }

    private void UpdateDisplay(int currentHealth, int maxHealth)
    {
        if (_progressBar != null)
        {
            _progressBar.MaxValue = maxHealth;
            _progressBar.Value = currentHealth;
        }
    }
}
