using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Entities.Components;

public partial class UnitVisualsComponent : Node
{
    [Export] private ProgressBar HealthBar { get; set; } = null!;
    [Export] private BaseUnit _unit = null!;
    [Export] private AnimatedSprite2D _animatedSprite = null!;
    [Export] private AnimationPlayer? _animationPlayer;

    [Export] private string IdleAnimation = "idle";
    [Export] private string MoveAnimation = "move";
    [Export] private string AttackAnimation = "attack";
    [Export] private string DieAnimation = "die";

    [Export] private string IdleFxAnimation = "";
    [Export] private string MoveFxAnimation = "";
    [Export] private string AttackFxAnimation = "";
    [Export] private string DieFxAnimation = "";

    private HealthComponent? _boundHealthComponent;
    private UnitStateMachine? _boundStateMachine;
    private bool _boundBaseHealthSignal;

    // TODO sync animations with attack speed
    public override void _Ready()
    {
        ResolveRuntimeReferences();

        if (!GodotObject.IsInstanceValid(_unit))
        {
            return;
        }

        if (!GodotObject.IsInstanceValid(HealthBar))
        {
            GD.PushWarning($"[{nameof(UnitVisualsComponent)}] HealthBar reference missing on node '{Name}'.");
        }

        _boundStateMachine = _unit.StateMachine;
        if (GodotObject.IsInstanceValid(_boundStateMachine))
        {
            _boundStateMachine!.StateChanged += OnUnitStateChanged;
            PlayStateAnimation(_boundStateMachine.CurrentState);
        }
        else
        {
            GD.PushWarning($"[{nameof(UnitVisualsComponent)}] UnitStateMachine is not valid on node '{Name}'. State animations will not play.");
        }

        // Prefer listening to HealthComponent directly
        _boundHealthComponent = _unit.HealthComponent;
        if (GodotObject.IsInstanceValid(_boundHealthComponent))
        {
            _boundHealthComponent!.HealthChanged += OnHealthChanged;
            // Initialize bar
            OnHealthChanged(_boundHealthComponent.CurrentHealth, _boundHealthComponent.MaxHealth);
        }
        else
        {
            // Fallback to BaseUnit signals
            _unit.HealthChanged += OnHealthChanged;
            _boundBaseHealthSignal = true;
            OnHealthChanged(_unit.MaxHealth, _unit.MaxHealth);
        }
    }

    private void ResolveRuntimeReferences()
    {
        if (!GodotObject.IsInstanceValid(_unit))
        {
            _unit = FindUnitAncestor() ?? _unit;
        }

        if (!GodotObject.IsInstanceValid(_unit))
        {
            return;
        }

        if (!GodotObject.IsInstanceValid(_animatedSprite))
        {
            _animatedSprite = _unit.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D") ?? _animatedSprite;
        }

        if (!GodotObject.IsInstanceValid(_animationPlayer))
        {
            _animationPlayer = _unit.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        }

        if (!GodotObject.IsInstanceValid(HealthBar))
        {
            HealthBar = _unit.GetNodeOrNull<ProgressBar>("HealthBar") ?? HealthBar;
        }
    }

    private BaseUnit? FindUnitAncestor()
    {
        Node? current = GetParent();
        while (current != null)
        {
            if (current is BaseUnit unit)
            {
                return unit;
            }

            current = current.GetParent();
        }

        return null;
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_boundHealthComponent))
        {
            _boundHealthComponent!.HealthChanged -= OnHealthChanged;
        }

        if (_boundBaseHealthSignal && GodotObject.IsInstanceValid(_unit))
        {
            _unit.HealthChanged -= OnHealthChanged;
            _boundBaseHealthSignal = false;
        }

        if (GodotObject.IsInstanceValid(_boundStateMachine))
        {
            _boundStateMachine!.StateChanged -= OnUnitStateChanged;
        }
    }

    private void OnHealthChanged(int newHealth, int maxHealth)
    {
        if (!GodotObject.IsInstanceValid(HealthBar))
        {
            return;
        }

        HealthBar.MaxValue = maxHealth;
        HealthBar.Value = newHealth;
        HealthBar.Visible = newHealth < maxHealth; // Hide if full
    }

    private void OnUnitStateChanged(int previousState, int newState)
    {
        PlayStateAnimation((UnitStateEnum)newState);
    }

    private void PlayStateAnimation(UnitStateEnum state)
    {
        // Protect against disposed objects in multiplayer contexts
        if (!GodotObject.IsInstanceValid(this) || !GodotObject.IsInstanceValid(_unit))
            return;

        var spriteAnimationName = GetSpriteAnimationName(state);
        GD.Print($"[{nameof(UnitVisualsComponent)}] Playing sprite animation '{spriteAnimationName}' for state '{state}' on unit '{_unit.Name}'.");
        if (GodotObject.IsInstanceValid(_animatedSprite) && !string.IsNullOrWhiteSpace(spriteAnimationName))
        {
            if (_animatedSprite.SpriteFrames != null && _animatedSprite.SpriteFrames.HasAnimation(spriteAnimationName))
            {
                _animatedSprite.Play(spriteAnimationName);
            }
            else
            {
                string unitName = GodotObject.IsInstanceValid(_unit) ? _unit.Name : "Unknown";
                GD.PushWarning($"[{nameof(UnitVisualsComponent)}] Missing sprite animation '{spriteAnimationName}' on unit '{unitName}'.");
            }
        }

        // AnimationPlayer is optional; skip FX if not assigned
        if (!GodotObject.IsInstanceValid(_animationPlayer))
            return;

        var fxAnimationName = GetFxAnimationName(state);
        if (string.IsNullOrWhiteSpace(fxAnimationName))
            return;

        if (_animationPlayer.HasAnimation(fxAnimationName))
        {
            _animationPlayer.Play(fxAnimationName);
        }
        else
        {
            string unitName = GodotObject.IsInstanceValid(_unit) ? _unit.Name : "Unknown";
            GD.PushWarning($"[{nameof(UnitVisualsComponent)}] Missing FX animation '{fxAnimationName}' on unit '{unitName}'.");
        }
    }

    private string GetSpriteAnimationName(UnitStateEnum state)
    {
        return state switch
        {
            UnitStateEnum.Idle => IdleAnimation,
            UnitStateEnum.Moving => MoveAnimation,
            UnitStateEnum.Attacking => AttackAnimation,
            UnitStateEnum.Dying => DieAnimation,
            _ => IdleAnimation
        };
    }

    private string GetFxAnimationName(UnitStateEnum state)
    {
        return state switch
        {
            UnitStateEnum.Idle => IdleFxAnimation,
            UnitStateEnum.Moving => MoveFxAnimation,
            UnitStateEnum.Attacking => AttackFxAnimation,
            UnitStateEnum.Dying => DieFxAnimation,
            _ => string.Empty
        };
    }
}
