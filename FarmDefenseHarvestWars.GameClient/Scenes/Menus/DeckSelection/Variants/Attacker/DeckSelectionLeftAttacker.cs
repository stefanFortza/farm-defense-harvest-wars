using FarmDefenseHarvestWars.Shared.Enums;

public partial class DeckSelectionLeftAttacker : DeckSelectionLeft
{
    protected override PlayerRole GetRole() => PlayerRole.Attacker;
}
