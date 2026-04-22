using System.Globalization;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scenes.UI.Components.SettingsSlider;

[Tool]
public partial class SettingsSlider : Control
{
    [Signal]
    public delegate void ValueChangedEventHandler(double value);

    [Export] private Label _titleLabel = null!;
    [Export] private HSlider _slider = null!;

    private string _titleText = "Setting";
    private double _minValue = 0.0;
    private double _maxValue = 1.0;
    private double _step = 0.01;
    private double _value = 0.8;

    [Export]
    public string TitleText
    {
        get => _titleText;
        set
        {
            _titleText = value;
            // Actualizăm vizual titlul direct din setter (funcționează și în editor)
            if (_titleLabel != null)
            {
                _titleLabel.Text = _titleText;
            }
        }
    }

    [Export]
    public double MinValue
    {
        get => _minValue;
        set => _minValue = value;
    }

    [Export]
    public double MaxValue
    {
        get => _maxValue;
        set => _maxValue = value;
    }

    [Export]
    public double Step
    {
        get => _step;
        set => _step = value;
    }

    [Export]
    public double Value
    {
        get => _value;
        set
        {
            _value = value;
            // Actualizăm valoarea slider-ului doar la runtime, dacă se modifică din cod
            if (!Engine.IsEditorHint() && _slider != null)
            {
                _slider.Value = ClampValue(_value);
            }
        }
    }

    public override void _Ready()
    {
        this.EnsureNotNull(_titleLabel, nameof(_titleLabel));
        this.EnsureNotNull(_slider, nameof(_slider));

        // Asigurăm afișarea textului la inițializare
        _titleLabel.Text = _titleText;

        // Izolarea logicii: se execută EXCLUSIV la rularea jocului, niciodată în editor
        if (!Engine.IsEditorHint())
        {
            ValidateRange();

            _slider.MinValue = _minValue;
            _slider.MaxValue = _maxValue;
            _slider.Step = _step;
            _slider.Value = ClampValue(_value);

            _slider.ValueChanged += OnSliderValueChanged;
        }
    }

    public override void _ExitTree()
    {
        // Dezabonarea se face doar dacă ne-am abonat (la runtime)
        if (!Engine.IsEditorHint() && _slider != null && GodotObject.IsInstanceValid(_slider))
        {
            _slider.ValueChanged -= OnSliderValueChanged;
        }

        _titleLabel = null!;
        _slider = null!;
    }

    private void OnSliderValueChanged(double newValue)
    {
        _value = ClampValue(newValue);
        EmitSignal(SignalName.ValueChanged, _value);
    }

    private void ValidateRange()
    {
        if (_maxValue < _minValue)
        {
            (_minValue, _maxValue) = (_maxValue, _minValue);
        }
    }

    private double ClampValue(double candidate)
    {
        return Mathf.Clamp(candidate, _minValue, _maxValue);
    }
}