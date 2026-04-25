using Godot;
using FarmDefenseHarvestWars.GameClient.Entities.Components;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base;

public partial class DefenderBase : Node2D, IInitializable<HealthComponent>
{
    [Export] public HurtboxComponent HurtboxComponent { get; private set; } = null!;
    [Export] public Node2D? HealthBar { get; private set; }

    public bool IsInitialized { get; private set; } = false;

    private HealthComponent _healthComponent = null!;

    public void Initialize(HealthComponent healthComponent)
    {
        _healthComponent = healthComponent;
        IsInitialized = true;
        
        // If Hurtbox is already assigned (which it should be via Export), initialize it immediately.
        // This handles cases where Initialize is called after DefenderBase._Ready.
        if (HurtboxComponent != null && _healthComponent != null)
        {
            HurtboxComponent.Initialize(_healthComponent);
        }

        if (HealthBar is IInitializable<HealthComponent> initializable)
        {
            initializable.Initialize(_healthComponent);
        }
    }

    public override void _Ready()
    {
        this.EnsureNotNull(HurtboxComponent, nameof(HurtboxComponent));
        
        // If we were already initialized before _Ready, make sure the Hurtbox is set up.
        if (IsInitialized && _healthComponent != null)
        {
            HurtboxComponent.Initialize(_healthComponent);
        }

        AddToGroup("DefenderBase");
    }
}
