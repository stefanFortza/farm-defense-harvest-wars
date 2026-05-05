using Godot;
using FarmDefenseHarvestWars.GameClient.Entities.Components;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base;

public partial class DefenderBase : Node2D, IInitializable<HealthComponent>
{
    [Export] public HurtboxComponent HurtboxComponent { get; private set; } = null!;
    [Export] public Node2D? HealthBar { get; private set; }
    [Export] public Node2D? VisualsContainer { get; private set; }

    public bool IsInitialized { get; private set; } = false;

    private HealthComponent _healthComponent = null!;
    private int _previousHealth;
    private Tween? _hitTween;
    private Vector2 _originalVisualsPos;

    public void Initialize(HealthComponent healthComponent)
    {
        if (_healthComponent != null)
        {
            _healthComponent.HealthChanged -= OnHealthChanged;
        }

        _healthComponent = healthComponent;
        IsInitialized = true;
        
        if (_healthComponent != null)
        {
            _healthComponent.HealthChanged += OnHealthChanged;
            _previousHealth = _healthComponent.CurrentHealth;
        }

        // If Hurtbox is already assigned (which it should be via Export), initialize it immediately.
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
        
        if (VisualsContainer != null)
        {
            _originalVisualsPos = VisualsContainer.Position;
        }
        else
        {
             _originalVisualsPos = Position;
        }

        // If we were already initialized before _Ready, make sure the Hurtbox is set up.
        if (IsInitialized && _healthComponent != null)
        {
            HurtboxComponent.Initialize(_healthComponent);
        }

        AddToGroup("DefenderBase");
    }

    public override void _ExitTree()
    {
        if (_healthComponent != null)
        {
            _healthComponent.HealthChanged -= OnHealthChanged;
        }
        _hitTween?.Kill();
    }

    private void OnHealthChanged(int current, int max)
    {
        if (current < _previousHealth)
        {
            PlayHitEffect();
        }
        _previousHealth = current;
    }

    public void PlayHitEffect()
    {
        _hitTween?.Kill();
        _hitTween = CreateTween();

        Node2D target = VisualsContainer ?? this;

        // 1. Flash Red
        target.Modulate = new Color(2f, 0.5f, 0.5f, 1f);
        _hitTween.TweenProperty(target, "modulate", Colors.White, 0.2f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

        // 2. Shake
        _hitTween.Parallel().TweenProperty(target, "position", _originalVisualsPos + new Vector2(4, 0), 0.05f)
            .SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
        _hitTween.TweenProperty(target, "position", _originalVisualsPos + new Vector2(-4, 0), 0.05f);
        _hitTween.TweenProperty(target, "position", _originalVisualsPos + new Vector2(2, 0), 0.05f);
        _hitTween.TweenProperty(target, "position", _originalVisualsPos, 0.05f);
    }
}
