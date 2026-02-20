using FarmDefenseHarvestWars.GameClient.Entities.Units.Base;
using Godot;

public partial class UnitVisualsComponent : Node
{
    [Export] public ProgressBar HealthBar { get; set; } = null!;
    private BaseUnit _unit = null!;

    public override void _Ready()
    {
        _unit = GetParentOrNull<BaseUnit>();

        // TODO HEALTBAR is not working
        HealthBar ??= _unit.GetNodeOrNull<ProgressBar>("HealthBar");

        if (_unit != null)
        {
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
    }

    private void OnHealthChanged(int newHealth, int maxHealth)
    {
        if (HealthBar != null)
        {
            HealthBar.MaxValue = maxHealth;
            HealthBar.Value = newHealth;
            HealthBar.Visible = newHealth < maxHealth; // Hide if full
        }
    }
}
