
using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MainMenuPages;

public partial class SettingsPageRight : MarginContainer
{

    private const string SettingsPath = "user://main_menu_settings.cfg";
    private static readonly int[] FpsOptions = [30, 60, 120, 0];


    [Export] public CheckButton FullscreenToggle { get; set; } = null!;
    [Export] public CheckButton VsyncToggle { get; set; } = null!;
    [Export] public OptionButton FpsCapOption { get; set; } = null!;

    private bool _isFullscreen;
    private bool _vsyncEnabled = true;
    private int _fpsCap = 60;

    public override void _Ready()
    {
        LoadSettings();
        ApplySettings();
        BindSignals();
    }

    public override void _ExitTree()
    {
        UnbindSignals();
    }

    private void BindSignals()
    {
        this.EnsureNotNull(FullscreenToggle, nameof(FullscreenToggle));
        this.EnsureNotNull(VsyncToggle, nameof(VsyncToggle));
        this.EnsureNotNull(FpsCapOption, nameof(FpsCapOption));

        SetupFpsOptions();
        RefreshUiControls();

        FullscreenToggle.Toggled += OnFullscreenToggled;
        VsyncToggle.Toggled += OnVsyncToggled;
        FpsCapOption.ItemSelected += OnFpsCapSelected;
    }


    private void UnbindSignals()
    {
        if (FullscreenToggle != null && GodotObject.IsInstanceValid(FullscreenToggle))
        {
            FullscreenToggle.Toggled -= OnFullscreenToggled;
        }

        if (VsyncToggle != null && GodotObject.IsInstanceValid(VsyncToggle))
        {
            VsyncToggle.Toggled -= OnVsyncToggled;
        }

        if (FpsCapOption != null && GodotObject.IsInstanceValid(FpsCapOption))
        {
            FpsCapOption.ItemSelected -= OnFpsCapSelected;
        }
    }

    private void RefreshUiControls()
    {
        FullscreenToggle.ButtonPressed = _isFullscreen;
        VsyncToggle.ButtonPressed = _vsyncEnabled;
        SelectFpsCapOption();
    }


    private void OnFullscreenToggled(bool pressed)
    {
        _isFullscreen = pressed;
        ApplyWindowMode();
        SaveSettings();
    }

    private void OnVsyncToggled(bool pressed)
    {
        _vsyncEnabled = pressed;
        ApplyVsyncMode();
        SaveSettings();
    }

    private void OnFpsCapSelected(long index)
    {
        _fpsCap = FpsCapOption!.GetItemMetadata((int)index).AsInt32();
        ApplyFpsCap();
        SaveSettings();
    }

    private void LoadSettings()
    {
        var config = new ConfigFile();
        if (config.Load(SettingsPath) != Error.Ok)
        {
            return;
        }

        _isFullscreen = config.GetValue("video", "fullscreen", false).AsBool();
        _vsyncEnabled = config.GetValue("video", "vsync", true).AsBool();
        _fpsCap = config.GetValue("video", "fps_cap", 60).AsInt32();
    }

    private void SaveSettings()
    {
        var config = new ConfigFile();
        config.Load(SettingsPath); // Load existing settings to preserve unchanged values
        config.SetValue("video", "fullscreen", _isFullscreen);
        config.SetValue("video", "vsync", _vsyncEnabled);
        config.SetValue("video", "fps_cap", _fpsCap);
        config.Save(SettingsPath);
    }

    private void ApplySettings()
    {
        ApplyWindowMode();
        ApplyVsyncMode();
        ApplyFpsCap();
    }


    private void ApplyWindowMode()
    {
        DisplayServer.WindowSetMode(_isFullscreen
            ? DisplayServer.WindowMode.Fullscreen
            : DisplayServer.WindowMode.Windowed);
    }

    private void ApplyVsyncMode()
    {
        DisplayServer.WindowSetVsyncMode(_vsyncEnabled
            ? DisplayServer.VSyncMode.Enabled
            : DisplayServer.VSyncMode.Disabled);
    }

    private void ApplyFpsCap()
    {
        Engine.MaxFps = _fpsCap;
    }


    private void SetupFpsOptions()
    {
        FpsCapOption.Clear();
        foreach (int fps in FpsOptions)
        {
            string label = fps == 0 ? "Unlimited" : $"{fps} FPS";
            FpsCapOption.AddItem(label);
            int index = FpsCapOption!.ItemCount - 1;
            FpsCapOption.SetItemMetadata(index, fps);
        }
    }

    private void SelectFpsCapOption()
    {
        int fallbackIndex = 1;
        for (int i = 0; i < FpsCapOption!.ItemCount; i++)
        {
            int value = FpsCapOption!.GetItemMetadata(i).AsInt32();
            if (value != _fpsCap)
            {
                continue;
            }

            FpsCapOption!.Select(i);
            return;
        }

        FpsCapOption!.Select(Mathf.Clamp(fallbackIndex, 0, FpsCapOption!.ItemCount - 1));
        _fpsCap = FpsCapOption!.GetItemMetadata(FpsCapOption!.GetSelectedId()).AsInt32();
    }
}
