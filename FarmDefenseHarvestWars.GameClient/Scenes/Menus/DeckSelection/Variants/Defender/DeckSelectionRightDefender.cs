using FarmDefenseHarvestWars.Shared.Enums;

public partial class DeckSelectionRightDefender : DeckSelectionRight
{
    protected override PlayerRole GetRole() => PlayerRole.Defender;
}
