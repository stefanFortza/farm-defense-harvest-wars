using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Entities.Components;

public partial class UnitVisualsComponent : Node
{
    [Export] private Range HealthBar { get; set; } = null!;
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

    private int _previousHealth;
    private Tween? _attackTween;
    private Tween? _hurtTween;
    private double _lastHurtTime;
    private const double HurtCooldown = 0.1;

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

        _unit.AttackStarted += OnUnitAttackStarted;
        _unit.HitImpact += OnUnitHitImpact;

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
            _previousHealth = _boundHealthComponent.CurrentHealth;
            // Initialize bar
            OnHealthChanged(_boundHealthComponent.CurrentHealth, _boundHealthComponent.MaxHealth);
        }
        else
        {
            // Fallback to BaseUnit signals
            _unit.HealthChanged += OnHealthChanged;
            _boundBaseHealthSignal = true;
            _previousHealth = _unit.MaxHealth;
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
        if (GodotObject.IsInstanceValid(_unit))
        {
            _unit.AttackStarted -= OnUnitAttackStarted;
            _unit.HitImpact -= OnUnitHitImpact;
        }

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

        _attackTween?.Kill();
        _hurtTween?.Kill();
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

        if (newHealth < _previousHealth)
        {
            // Fallback for damage not triggered by direct Melee RPC (e.g. status effects)
            double currentTime = Time.GetTicksMsec() / 1000.0;
            if (currentTime - _lastHurtTime > HurtCooldown)
            {
                PlayHurtAnimation();
            }
        }

        _previousHealth = newHealth;
    }

    private void OnUnitAttackStarted()
    {
        bool isDefender = _unit.Data.Role == FarmDefenseHarvestWars.Shared.Enums.PlayerRole.Defender;
        bool hasAttackAnimation = _animatedSprite.SpriteFrames != null && _animatedSprite.SpriteFrames.HasAnimation(AttackAnimation);

        if (isDefender)
        {
            PlayProceduralAttackAnimation();
        }
        else if (hasAttackAnimation)
        {
            _animatedSprite.Stop();
            _animatedSprite.Play(AttackAnimation);
        }
    }

    private void OnUnitHitImpact(NodePath targetPath)
    {
        if (targetPath.IsEmpty) return;

        var target = GetNodeOrNull(targetPath);
        if (target is BaseUnit targetUnit && GodotObject.IsInstanceValid(targetUnit))
        {
            foreach (var child in targetUnit.GetChildren())
            {
                if (child is UnitVisualsComponent targetVisuals)
                {
                    targetVisuals.PlayHurtAnimation();
                    break;
                }
            }
        }
    }

    private void OnUnitStateChanged(int previousState, int newState)
    {
        PlayStateAnimation((UnitStateEnum)newState);
    }

    private void PlayProceduralAttackAnimation()
    {
        if (!GodotObject.IsInstanceValid(_animatedSprite)) return;

        bool isRanged = _unit.Data.ProjectileScene != null;

        _attackTween?.Kill();
        _attackTween = CreateTween();

        float attackSpeed = (float)Mathf.Max(0.1f, _unit.Data.AttackSpeed);
        float totalDuration = 1.0f / attackSpeed;

        if (isRanged)
        {
            PlayProceduralRangedAttack(totalDuration);
        }
        else
        {
            PlayProceduralMeleeAttack(totalDuration);
        }
    }

    private void PlayProceduralMeleeAttack(float totalDuration)
    {
        // Melee Lunge: 
        // 0% - 20%: Anticipation (Subtle Squash)
        // 20% - 50%: Lunge forward (Subtle Stretch). Peak at 50%.
        // 50% - 100%: Recovery (Back)
        float antTime = totalDuration * 0.2f;
        float strikeTime = totalDuration * 0.3f; // Ends at 50%
        float recoveryTime = totalDuration * 0.5f;

        Vector2 originalPos = _animatedSprite.Position;

        // Use base forward without FacingDirection, because the parent node handles the flip scale
        Vector2 baseForward = (_unit is AttackerUnit) ? Vector2.Left : Vector2.Right;
        Vector2 forwardOffset = baseForward * 12f;

        // 1. Anticipation: Un squash foarte subtil (5%)
        _attackTween.TweenProperty(_animatedSprite, "scale", new Vector2(0.95f, 1.05f), antTime)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        _attackTween.Parallel().TweenProperty(_animatedSprite, "position", originalPos - baseForward * 2f, antTime)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

        // 2. Strike: Un stretch ferm, dar controlat (10%)
        _attackTween.TweenProperty(_animatedSprite, "position", originalPos + forwardOffset, strikeTime)
            .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
        _attackTween.Parallel().TweenProperty(_animatedSprite, "scale", new Vector2(1.1f, 0.9f), strikeTime)
            .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);

        // Lean into the hit (Positive rotation leans forward relative to natural face)
        float rotationAngle = Mathf.DegToRad(8f);
        _attackTween.Parallel().TweenProperty(_animatedSprite, "rotation", rotationAngle, strikeTime)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

        // 3. Recovery: Fără Elastic. Folosim Back pentru o revenire cu un singur rebound.
        _attackTween.TweenProperty(_animatedSprite, "position", originalPos, recoveryTime)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        _attackTween.Parallel().TweenProperty(_animatedSprite, "scale", new Vector2(1.0f, 1.0f), recoveryTime)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        _attackTween.Parallel().TweenProperty(_animatedSprite, "rotation", 0f, recoveryTime)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
    }

    private void PlayProceduralRangedAttack(float totalDuration)
    {
        // Ranged "Recoil":
        // 0% - 50%: Build up/Charge. Lean forward slightly in the direction of the shot.
        // 50%: Flash + Fire + Snap Back (Recoil opposite to shot direction)
        // 50% - 100%: Recovery return
        float chargeTime = totalDuration * 0.5f;
        float recoveryTime = totalDuration * 0.5f;

        Vector2 originalPos = _animatedSprite.Position;

        // Use base forward without FacingDirection, because the parent node handles the flip scale
        Vector2 baseForward = (_unit is AttackerUnit) ? Vector2.Left : Vector2.Right;

        // Offset in the direction of the target (forward)
        Vector2 anticipationOffset = baseForward * 2f;
        // Offset away from the target (backward)
        Vector2 recoilOffset = -baseForward * 6f;

        // 1. Charge Up: Subtle squash (10%) and lean forward (anticipation)
        _attackTween.TweenProperty(_animatedSprite, "scale", new Vector2(1.1f, 0.9f), chargeTime)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        _attackTween.Parallel().TweenProperty(_animatedSprite, "position", originalPos + anticipationOffset + new Vector2(0, 1f), chargeTime)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);

        // 2. Action at 50%: Flash (Immediately after chargeTime finishes)
        _attackTween.TweenCallback(Callable.From(() =>
        {
            _animatedSprite.Modulate = new Color(2f, 2f, 2f, 1f);
            var flashTween = CreateTween();
            flashTween.TweenProperty(_animatedSprite, "modulate", Colors.White, 0.2f);
        }));

        // 3. Fire Response (Recoil): Snap Backward + Subtle Stretch (5%)
        _attackTween.TweenProperty(_animatedSprite, "position", originalPos + recoilOffset, recoveryTime * 0.2f)
            .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
        _attackTween.Parallel().TweenProperty(_animatedSprite, "scale", new Vector2(0.95f, 1.05f), recoveryTime * 0.2f)
            .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);

        // 4. Recovery: Return to original position using Back for a clean finish
        _attackTween.TweenProperty(_animatedSprite, "position", originalPos, recoveryTime * 0.8f)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        _attackTween.Parallel().TweenProperty(_animatedSprite, "scale", new Vector2(1.0f, 1.0f), recoveryTime * 0.8f)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    public void PlayHurtAnimation()
    {
        if (!GodotObject.IsInstanceValid(_animatedSprite)) return;

        _lastHurtTime = Time.GetTicksMsec() / 1000.0;

        _hurtTween?.Kill();
        _hurtTween = CreateTween();

        _animatedSprite.Modulate = new Color(2f, 0.7f, 0.7f, 1f);
        _hurtTween.TweenProperty(_animatedSprite, "modulate", Colors.White, 0.15f);

        // Un "Ouch!" mult mai scurt și reținut (5% diferență)
        _hurtTween.Parallel().TweenProperty(_animatedSprite, "scale", new Vector2(1.05f, 0.95f), 0.05f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        _hurtTween.TweenProperty(_animatedSprite, "scale", new Vector2(1.0f, 1.0f), 0.1f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
    }

    private void PlayStateAnimation(UnitStateEnum state)
    {
        // Protect against disposed objects in multiplayer contexts
        if (!GodotObject.IsInstanceValid(this) || !GodotObject.IsInstanceValid(_unit))
            return;

        var spriteAnimationName = GetSpriteAnimationName(state);
        if (GodotObject.IsInstanceValid(_animatedSprite) && !string.IsNullOrWhiteSpace(spriteAnimationName))
        {
            if (_animatedSprite.SpriteFrames != null)
            {
                if (_animatedSprite.SpriteFrames.HasAnimation(spriteAnimationName))
                {
                    if (state == UnitStateEnum.Attacking)
                    {
                        _animatedSprite.SpeedScale = (float)_unit.Data.AttackSpeed;
                    }
                    else
                    {
                        _animatedSprite.SpeedScale = 1.0f; // Reset speed for other states
                    }

                    _animatedSprite.Play(spriteAnimationName);
                }
                else
                {
                    // Fallback to "default" or first available animation without pushing warnings every frame
                    if (_animatedSprite.SpriteFrames.HasAnimation("default"))
                        _animatedSprite.Play("default");
                    else if (_animatedSprite.SpriteFrames.GetAnimationNames().Length > 0)
                        _animatedSprite.Play(_animatedSprite.SpriteFrames.GetAnimationNames()[0]);
                }
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
