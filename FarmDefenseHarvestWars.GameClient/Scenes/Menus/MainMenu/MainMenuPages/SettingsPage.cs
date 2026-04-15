using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MainMenuPages;

public partial class SettingsPage : MarginContainer
{
    private const string SettingsPath = "user://main_menu_settings.cfg";

    [Export] public HSlider MasterVolumeSlider { get; set; } = null!;
    [Export] public CheckButton FullscreenToggle { get; set; } = null!;
    [Export] public Label VolumeValueLabel { get; set; } = null!;

    private float _masterVolumeLinear = 0.8f;
    private bool _isFullscreen;
    private bool _updatingUi;

    public override void _Ready()
    {
        LoadSettings();
        ApplySettings();
        RefreshUi();

        if (MasterVolumeSlider != null)
        {
            MasterVolumeSlider.ValueChanged += OnVolumeChanged;
        }

        if (FullscreenToggle != null)
        {
            FullscreenToggle.Toggled += OnFullscreenToggled;
        }
    }

    public override void _ExitTree()
    {
        if (MasterVolumeSlider != null)
        {
            MasterVolumeSlider.ValueChanged -= OnVolumeChanged;
        }

        if (FullscreenToggle != null)
        {
            FullscreenToggle.Toggled -= OnFullscreenToggled;
        }
    }

    private void OnVolumeChanged(double value)
    {
        if (_updatingUi)
        {
            return;
        }

        _masterVolumeLinear = Mathf.Clamp((float)value, 0f, 1f);
        ApplyMasterVolume();
        RefreshVolumeLabel();
        SaveSettings();
    }

    private void OnFullscreenToggled(bool pressed)
    {
        if (_updatingUi)
        {
            return;
        }

        _isFullscreen = pressed;
        ApplyWindowMode();
        SaveSettings();
    }

    private void LoadSettings()
    {
        var config = new ConfigFile();
        if (config.Load(SettingsPath) != Error.Ok)
        {
            return;
        }

        _masterVolumeLinear = Mathf.Clamp(config.GetValue("audio", "master_volume", 0.8f).AsSingle(), 0f, 1f);
        _isFullscreen = config.GetValue("video", "fullscreen", false).AsBool();
    }

    private void SaveSettings()
    {
        var config = new ConfigFile();
        config.SetValue("audio", "master_volume", _masterVolumeLinear);
        config.SetValue("video", "fullscreen", _isFullscreen);
        config.Save(SettingsPath);
    }

    private void ApplySettings()
    {
        ApplyMasterVolume();
        ApplyWindowMode();
    }

    private void ApplyMasterVolume()
    {
        int masterBus = AudioServer.GetBusIndex("Master");
        if (masterBus < 0)
        {
            return;
        }

        float db = _masterVolumeLinear <= 0.0001f ? -80f : Mathf.LinearToDb(_masterVolumeLinear);
        AudioServer.SetBusVolumeDb(masterBus, db);
    }

    private void ApplyWindowMode()
    {
        DisplayServer.WindowSetMode(_isFullscreen
            ? DisplayServer.WindowMode.Fullscreen
            : DisplayServer.WindowMode.Windowed);
    }

    private void RefreshUi()
    {
        _updatingUi = true;

        if (MasterVolumeSlider != null)
        {
            MasterVolumeSlider.Value = _masterVolumeLinear;
        }

        if (FullscreenToggle != null)
        {
            FullscreenToggle.ButtonPressed = _isFullscreen;
        }

        RefreshVolumeLabel();
        _updatingUi = false;
    }

    private void RefreshVolumeLabel()
    {
        if (VolumeValueLabel != null)
        {
            VolumeValueLabel.Text = $"{Mathf.RoundToInt(_masterVolumeLinear * 100f)}%";
        }
    }
}
