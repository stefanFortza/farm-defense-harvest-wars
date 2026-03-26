using FarmDefenseHarvestWars.Shared.Enums;

public partial class DeckSelectionRightAttacker : DeckSelectionRight
{
    protected override PlayerRole GetRole() => PlayerRole.Attacker;
}
