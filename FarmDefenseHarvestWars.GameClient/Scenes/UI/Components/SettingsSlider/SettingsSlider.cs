using System.Globalization;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scenes.UI.Components.SettingsSlider;

[Tool]
public partial class SettingsSlider : Control
{
    private const string GrabberBlendShaderPath = "res://Scenes/UI/Components/SettingsSlider/SettingsSliderGrabberBlend.gdshader";

    [Signal]
    public delegate void ValueChangedEventHandler(double value);

    [Export] private Label _titleLabel = null!;
    [Export] private HSlider _slider = null!;

    private string _titleText = "Setting";
    private double _minValue;
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
            ApplyTitle();
        }
    }

    [Export]
    public double MinValue
    {
        get => _minValue;
        set
        {
            _minValue = value;
            ValidateRange();
            ApplySliderConfig();
            ApplyValue(false);
        }
    }

    [Export]
    public double MaxValue
    {
        get => _maxValue;
        set
        {
            _maxValue = value;
            ValidateRange();
            ApplySliderConfig();
            ApplyValue(false);
        }
    }

    [Export]
    public double Step
    {
        get => _step;
        set
        {
            _step = value;
            ApplySliderConfig();
        }
    }

    [Export]
    public double Value
    {
        get => _value;
        set
        {
            _value = ClampValue(value);
            ApplyValue(false);
        }
    }


    public override void _Ready()
    {
        this.EnsureNotNull(_titleLabel, nameof(_titleLabel));
        this.EnsureNotNull(_slider, nameof(_slider));

        _slider.ValueChanged += OnSliderValueChanged;

        ValidateRange();
        ApplyTitle();
        ApplySliderConfig();
        ApplyValue(false);
    }

    public override void _ExitTree()
    {
        _slider.ValueChanged -= OnSliderValueChanged;
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

    private void ApplyTitle()
    {
        _titleLabel.Text = _titleText;
    }

    private void ApplySliderConfig()
    {
        _slider.MinValue = _minValue;
        _slider.MaxValue = _maxValue;
        _slider.Step = _step;
    }

    private void ApplyValue(bool emitSignal)
    {
        _slider.Value = _value;

        if (emitSignal)
        {
            EmitSignal(SignalName.ValueChanged, _value);
        }
    }





}
