using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;

namespace FarmDefenseHarvestWars.GameClient.UI;

/// <summary>
/// Script de test pentru verificarea debouncing-ului si versionarii la salvarea deck-ului.
/// Demonstreaza ca multiple cereri rapide pe client rezulta intr-un numar minim de request-uri pe server.
/// </summary>
public partial class DeckStressTest : Button
{
    [Export] private PlayerRole _testRole = PlayerRole.Attacker;
    [Export] private UnitRegistry _registry = null!;

    public override void _Ready()
    {
        // Conectam semnalul Pressed la metoda noastra
        Pressed += OnStressTestPressed;

        if (_registry == null)
        {
            GD.PrintErr("[DeckStressTest] UnitRegistry is missing! Please assign it in the Inspector.");
        }
    }

    private async void OnStressTestPressed()
    {
        if (DeckService.Instance == null)
        {
            GD.PrintErr("[DeckStressTest] DeckService Instance is null!");
            return;
        }

        GD.PrintRich("[color=yellow]>>> START STRESS TEST: Trimitere 20 cereri de salvare deck rapid...[/color]");

        // Simulam un deck de test (asigura-te ca unitatile sunt compatibile cu rolul ales)
        var testDeck = new List<UnitType> { UnitType.Skeleton, UnitType.OrcPeon };

        for (int i = 1; i <= 20; i++)
        {
            GD.Print($"[Client] Triggering save version {i}...");

            // Apelam DeckService-ul care gestioneaza coada si versionarea
            DeckService.Instance.SubmitDeckSaveForRole(_testRole, testDeck, _registry);

            // Task.Delay mic pentru a simula input rapid fara a bloca complet thread-ul
            await Task.Delay(10);
        }

        GD.PrintRich("[color=green]>>> STRESS TEST FINISHED pe partea de UI. Verifica consola Backend-ului pentru numarul de request-uri POST.[/color]");
    }
}
