using Godot;
using System;

public partial class UnitVisuals : Node
{
    [Export] public ProgressBar HealthBar { get; set; }
    private BaseUnit _unit;

    public override void _Ready()
    {
        _unit = GetParent<BaseUnit>();

        if (HealthBar == null)
        {
            HealthBar = _unit.GetNodeOrNull<ProgressBar>("HealthBar");
        }

        if (_unit != null)
        {
            _unit.HealthChanged += OnHealthChanged;
            // Initialize bar
            OnHealthChanged(_unit.MaxHealth, _unit.MaxHealth);
        }
    }

    private void OnHealthChanged(int newHealth, int maxHealth)
    {
        if (HealthBar != null)
        {
            HealthBar.MaxValue = maxHealth;
            HealthBar.Value = newHealth;
            HealthBar.Visible = newHealth < maxHealth; // Hide if full? Or always show?
        }
    }
}
