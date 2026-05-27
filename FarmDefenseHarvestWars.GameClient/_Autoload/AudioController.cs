using Godot;
using System;
using System.Collections.Generic;

public partial class AudioController : Node
{
    public static AudioController Instance { get; private set; } = null!;

    [Export] public AudioStream? MenuMusic { get; set; }
    [Export] public AudioStream? GameplayMusic { get; set; }

    private AudioStreamPlayer _musicPlayer = null!;
    private Node _sfxContainer = null!;

    public override void _Ready()
    {
        Instance = this;
        
        _musicPlayer = new AudioStreamPlayer();
        _musicPlayer.Bus = "Music";
        AddChild(_musicPlayer);

        _sfxContainer = new Node();
        _sfxContainer.Name = "SFXContainer";
        AddChild(_sfxContainer);

        // Apply saved volume settings immediately
        ApplySavedVolumeSettings();

        // Auto-play menu music if available when starting
        PlayMenuMusic();
    }

    private void ApplySavedVolumeSettings()
    {
        const string settingsPath = "user://main_menu_settings.cfg";
        var config = new ConfigFile();
        
        if (config.Load(settingsPath) != Error.Ok)
        {
            return;
        }

        float master = config.GetValue("audio", "master_volume", 0.8f).AsSingle();
        float music = config.GetValue("audio", "music_volume", 0.8f).AsSingle();
        float sfx = config.GetValue("audio", "sfx_volume", 0.8f).AsSingle();

        SetBusVolume("Master", master);
        SetBusVolume("Music", music);
        SetBusVolume("SFX", sfx);
    }

    private void SetBusVolume(string busName, float linearValue)
    {
        int index = AudioServer.GetBusIndex(busName);
        if (index != -1)
        {
            float db = linearValue <= 0.0001f ? -80f : Mathf.LinearToDb(linearValue);
            AudioServer.SetBusVolumeDb(index, db);
        }
    }

    public void PlayMenuMusic()
    {
        if (MenuMusic != null)
        {
            PlayMusic(MenuMusic);
        }
    }

    public void PlayGameplayMusic()
    {
        if (GameplayMusic != null)
        {
            PlayMusic(GameplayMusic);
        }
    }

    private void PlayMusic(AudioStream stream)
    {
        if (_musicPlayer.Stream == stream && _musicPlayer.Playing)
        {
            return;
        }

        _musicPlayer.Stop();
        _musicPlayer.Stream = stream;
        _musicPlayer.Play();
    }

    public void StopMusic()
    {
        _musicPlayer.Stop();
    }

    public void PlaySfx(AudioStream stream, float pitchRange = 0.1f, float volumeDb = 0f)
    {
        if (stream == null) return;

        var player = new AudioStreamPlayer();
        player.Stream = stream;
        player.Bus = "SFX";
        player.VolumeDb = volumeDb;
        
        if (pitchRange > 0)
        {
            player.PitchScale = (float)GD.RandRange(1.0f - pitchRange, 1.0f + pitchRange);
        }

        _sfxContainer.AddChild(player);
        player.Play();
        
        player.Finished += () => player.QueueFree();
    }

    public void PlaySfx(string path, float pitchRange = 0.1f, float volumeDb = 0f)
    {
        var stream = GD.Load<AudioStream>(path);
        PlaySfx(stream, pitchRange, volumeDb);
    }
}
