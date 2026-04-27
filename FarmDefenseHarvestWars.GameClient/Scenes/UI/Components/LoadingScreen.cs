using Godot;
using System.Threading.Tasks;

public partial class LoadingScreen : CanvasLayer
{
    [Export] private Label _statusLabel = null!;
    [Export] private ProgressBar _progressBar = null!;
    [Export] private Control _spinner = null!;

    public override void _Ready()
    {
        Layer = 120; // High layer to cover everything
        if (_spinner != null)
        {
            _spinner.PivotOffset = _spinner.Size / 2;
        }
        
        if (_progressBar != null)
        {
            _progressBar.Value = 0;
        }
    }

    public override void _Process(double delta)
    {
        if (_spinner != null)
        {
            _spinner.Rotation += (float)delta * 5.0f;
        }
        
        if (_statusLabel != null)
        {
            float pulse = (Mathf.Sin((float)Time.GetTicksMsec() * 0.005f) + 1.0f) * 0.5f;
            _statusLabel.SelfModulate = new Color(1, 1, 1, 0.7f + pulse * 0.3f);
        }

        if (_progressBar != null && _progressBar.Value < 90)
        {
            _progressBar.Value += delta * 15.0f; // Fake progress until scene loads
        }
    }

    public void SetStatus(string text)
    {
        if (_statusLabel != null)
            _statusLabel.Text = text;
    }

    public void SetProgress(float value)
    {
        if (_progressBar != null)
            _progressBar.Value = value;
    }
}
