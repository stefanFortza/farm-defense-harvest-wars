using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base;

public partial class AttackerUnit : BaseUnit
{
    // These now pull from Data if available, or use defaults
    public float Speed => Data?.Speed ?? 100.0f;
    public int Damage => Data?.Damage ?? 10;
    public float AttackSpeed => Data?.AttackSpeed ?? 1.0f;

    public override void _Ready()
    {
        base._Ready();

        if (IsMultiplayerAuthority())
        {
            StateMachine.Start(UnitStateEnum.Moving);
        }
    }

    protected override void RegisterStates()
    {
        base.RegisterStates();
        StateMachine.RegisterState(UnitStateEnum.Moving, new MovingState(this));
    }

    protected override UnitStateEnum GetInitialState()
    {
        return UnitStateEnum.Moving; // Attackers start moving immediately
    }
}
