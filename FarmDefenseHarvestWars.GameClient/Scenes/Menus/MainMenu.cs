using Godot;
using System;

public partial class MainMenu : Control
{
    [Export] public Label WelcomeLabel { get; set; } = null!;
    [Export] public Label GoldLabel { get; set; } = null!;
    [Export] public Button PlayButton { get; set; } = null!;
    [Export] public Button LogoutButton { get; set; } = null!;

    // Placeholder pentru sprite-uri (le vei seta din Inspector sau din cod)
    [Export] public TextureRect GoldIcon { get; set; } = null!;
    [Export] public TextureRect LevelIcon { get; set; } = null!;

    public override void _Ready()
    {
        // Conectăm semnalele butoanelor
        PlayButton.Pressed += OnPlayPressed;
        LogoutButton.Pressed += OnLogoutPressed;

        // Actualizăm UI-ul cu datele din GameState
        UpdateUI();

        // Ascultăm modificările de profil (ex: dacă se actualizează banii în fundal)
        GameState.Instance.ProfileUpdated += UpdateUI;
    }

    public override void _ExitTree()
    {
        // Deconectăm semnalul pentru a evita memory leaks
        if (GameState.Instance != null)
        {
            GameState.Instance.ProfileUpdated -= UpdateUI;
        }
    }

    private void UpdateUI()
    {
        if (GameState.Instance.IsLoggedIn && GameState.Instance.CurrentProfile != null)
        {
            var profile = GameState.Instance.CurrentProfile;
            WelcomeLabel.Text = $"Salut, {profile.Email}!";
            GoldLabel.Text = $"Aur: {profile.Gold} | Nivel: {profile.Level}";
        }
        else
        {
            WelcomeLabel.Text = "Nu ești logat.";
            GoldLabel.Text = "";
        }
    }

    private void OnPlayPressed()
    {
        // Aici vom încărca scena de joc
        GetTree().ChangeSceneToFile("res://Scenes/Gameplay/FarmMap.tscn");
    }

    private void OnLogoutPressed()
    {
        NetworkManager.Instance.Logout();
    }
}
