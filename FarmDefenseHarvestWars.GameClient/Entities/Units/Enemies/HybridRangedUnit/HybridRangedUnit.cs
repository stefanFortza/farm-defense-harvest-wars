using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Enemies;

public partial class HybridRangedUnit : AttackerUnit
{
    protected override void RegisterStates()
    {
        base.RegisterStates();
        StateMachine.RegisterState(UnitStateEnum.Attacking, new HybridRangedAttackState(this));
    }
}
