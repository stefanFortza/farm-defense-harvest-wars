using Godot;

public partial class ResourcePanel : PanelContainer
{
    [Export] private Label _valueLabel = null!;

    public override void _Ready()
    {
        _valueLabel ??= GetNodeOrNull<Label>("Margin/HBox/ValueLabel");
    }

    public void UpdateDisplay(int amount)
    {
        if (_valueLabel == null)
        {
            return;
        }

        _valueLabel.Text = amount.ToString();
    }
}
