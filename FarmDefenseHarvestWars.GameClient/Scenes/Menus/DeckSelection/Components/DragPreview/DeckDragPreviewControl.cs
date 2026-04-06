using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;

public partial class DeckDragPreviewControl : PanelContainer
{
    [Export] private TextureRect _icon = null!;

    public override void _Ready()
    {
        this.EnsureNotNull(_icon, nameof(_icon));
    }

    public void Setup(Texture2D texture)
    {
        _icon.Texture = texture;
    }
}
