using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scenes.UI.Components.SettingsSlider;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MainMenuPages;

public partial class SettingsPageLeft : MarginContainer
{

    private const string SettingsPath = "user://main_menu_settings.cfg";
    private static readonly int[] FpsOptions = [30, 60, 120, 0];

    [Export] public SettingsSlider MasterVolumeSlider { get; set; } = null!;
    [Export] public SettingsSlider MusicVolumeSlider { get; set; } = null!;
    [Export] public SettingsSlider SfxVolumeSlider { get; set; } = null!;

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
        this.EnsureNotNull(MasterVolumeSlider, nameof(MasterVolumeSlider));
        this.EnsureNotNull(MusicVolumeSlider, nameof(MusicVolumeSlider));
        this.EnsureNotNull(SfxVolumeSlider, nameof(SfxVolumeSlider));


        MasterVolumeSlider.ValueChanged += OnMasterVolumeChanged;
        MusicVolumeSlider.ValueChanged += OnMusicVolumeChanged;
        SfxVolumeSlider.ValueChanged += OnSfxVolumeChanged;
    }


    private void UnbindSignals()
    {
        MasterVolumeSlider.ValueChanged -= OnMasterVolumeChanged;
        MusicVolumeSlider.ValueChanged -= OnMusicVolumeChanged;
        SfxVolumeSlider.ValueChanged -= OnSfxVolumeChanged;
    }


    private void OnMasterVolumeChanged(double value)
    {
        ApplyMasterVolume();
        SaveSettings();
    }

    private void OnMusicVolumeChanged(double value)
    {
        ApplyMusicVolume();
        SaveSettings();
    }

    private void OnSfxVolumeChanged(double value)
    {
        ApplySfxVolume();
        SaveSettings();
    }



    private void LoadSettings()
    {
        var config = new ConfigFile();
        if (config.Load(SettingsPath) != Error.Ok)
        {
            return;
        }

        MasterVolumeSlider.Value = Mathf.Clamp(config.GetValue("audio", "master_volume", 0.8f).AsSingle(), 0f, 1f);
        MusicVolumeSlider.Value = Mathf.Clamp(config.GetValue("audio", "music_volume", 0.8f).AsSingle(), 0f, 1f);
        SfxVolumeSlider.Value = Mathf.Clamp(config.GetValue("audio", "sfx_volume", 0.8f).AsSingle(), 0f, 1f);
    }

    private void SaveSettings()
    {
        var config = new ConfigFile();
        config.Load(SettingsPath); // Load existing settings to preserve unchanged values
        config.SetValue("audio", "master_volume", MasterVolumeSlider.Value);
        config.SetValue("audio", "music_volume", MusicVolumeSlider.Value);
        config.SetValue("audio", "sfx_volume", SfxVolumeSlider.Value);
        config.Save(SettingsPath);
    }

    private void ApplySettings()
    {
        ApplyMasterVolume();
        ApplyMusicVolume();
        ApplySfxVolume();
    }

    private void ApplyMasterVolume()
    {
        TrySetBusVolume(MasterVolumeSlider.Value, "Master", "MasterBus", "MasterVolume");
    }

    private void ApplyMusicVolume()
    {
        TrySetBusVolume(MusicVolumeSlider.Value, "Music", "MusicBus", "MusicVolume");
    }

    private void ApplySfxVolume()
    {
        TrySetBusVolume(SfxVolumeSlider.Value, "SFX", "SfxBus", "SfxVolume");
    }

    private static void TrySetBusVolume(double linear, params string[] candidateNames)
    {
        for (int i = 0; i < candidateNames.Length; i++)
        {
            int bus = AudioServer.GetBusIndex(candidateNames[i]);
            if (bus < 0)
            {
                continue;
            }

            double db = linear <= 0.0001f ? -80f : Mathf.LinearToDb(linear);
            AudioServer.SetBusVolumeDb(bus, (float)db);
            break;
        }
    }

}
