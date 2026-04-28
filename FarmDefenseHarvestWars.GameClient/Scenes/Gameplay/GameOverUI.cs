using Godot;
using System;
using System.Threading.Tasks;
using FarmDefenseHarvestWars.Shared.Models.Game;
using Refit;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Gameplay;

public partial class GameOverUI : CanvasLayer
{
    [Export] private Label _winnerLabel = null!;
    [Export] private Label _rewardLabel = null!;
    [Export] private Control _rewardContainer = null!;

    public override void _Ready()
    {
        if (_rewardContainer != null)
        {
            _rewardContainer.Hide();
        }

        _ = FetchRewardsAsync();
    }

    public void SetWinner(string winnerText)
    {
        if (_winnerLabel != null)
        {
            _winnerLabel.Text = $"Game Over\n{winnerText}";
        }
    }

    private async Task FetchRewardsAsync()
    {
        var gameState = GameState.Instance;
        if (gameState == null || string.IsNullOrEmpty(gameState.MatchId))
        {
            GD.PrintErr($"[GameOverUI] MatchId missing or invalid in GameState. Current ID: '{gameState?.MatchId ?? "null"}'");
            return;
        }

        // Try to get token from GameState first, then fallback to NetworkBootstrap
        string token = gameState.AccessToken;
        if (string.IsNullOrEmpty(token))
        {
            token = NetworkBootstrap.Instance?.AccessToken ?? "";
        }

        GD.Print($"[GameOverUI] Fetching rewards for match: {gameState.MatchId} (Token present: {!string.IsNullOrEmpty(token)})");
        
        // Ensure NetworkBootstrap has the token for its internal ApiClient
        if (NetworkBootstrap.Instance != null && !string.IsNullOrEmpty(token))
        {
            NetworkBootstrap.Instance.AccessToken = token;
        }

        int maxRetries = 5;
        int delayMs = 1500;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                GD.Print($"[GameOverUI] Attempt {i + 1} to fetch rewards...");
                var reward = await NetworkBootstrap.Instance.ApiClient.GetMatchRewardAsync(gameState.MatchId);
                
                if (reward != null)
                {
                    DisplayRewards(reward);
                    // Also update local profile if we want immediate feedback in UI after returning to menu
                    await NetworkBootstrap.Instance.Menu.GetProfileAsync();
                    return; // Success!
                }
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                GD.Print($"[GameOverUI] Rewards not ready yet (404). Retrying in {delayMs}ms...");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[GameOverUI] Failed to fetch rewards: {ex.Message}");
            }

            await Task.Delay(delayMs);
        }

        if (_rewardLabel != null)
        {
            _rewardLabel.Text = "Rewards are taking too long. Check your profile in the main menu.";
            _rewardContainer?.Show();
        }
    }

    private void DisplayRewards(MatchRewardDto reward)
    {
        if (_rewardLabel != null)
        {
            string resultText = reward.IsWin ? "Victory!" : "Defeat";
            if (reward.IsAborted) resultText = "Match Aborted";
            
            _rewardLabel.Text = $"{resultText}\n" +
                               $"Gold: +{reward.GoldEarned}\n" +
                               $"XP: +{reward.XpEarned}\n" +
                               $"Total Gold: {reward.TotalGoldNow}\n" +
                               $"Level: {reward.TotalLevelNow} ({reward.TotalXpNow} XP)";
        }
        
        _rewardContainer?.Show();
    }

    public void OnBackToMenuPressed()
    {
        GD.Print("[GameOverUI] Back to Menu pressed.");
        
        // Clear match state
        if (GameState.Instance != null)
        {
            GameState.Instance.SetMatchDecks("", [], []);
        }

        // Cleanup networking before leaving
        if (Multiplayer.MultiplayerPeer != null)
        {
            Multiplayer.MultiplayerPeer.Close();
        }
        
        GetTree().ChangeSceneToFile("res://Scenes/Menus/MainMenu/MainMenu.tscn");
    }
}
