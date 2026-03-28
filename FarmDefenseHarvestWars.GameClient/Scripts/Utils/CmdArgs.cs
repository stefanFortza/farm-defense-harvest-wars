using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FarmDefenseHarvestWars.Shared.Enums;

namespace FarmDefenseHarvestWars.GameClient.Scripts.Utils;

public static class CmdArgs
{
    public static string? Email { get; private set; }
    public static string? Password { get; private set; }
    public static bool IsServer { get; private set; }
    public static int? Port { get; private set; }
    public static string? MatchId { get; private set; }
    public static IReadOnlyList<UnitType>? DefenderDeck { get; private set; }
    public static IReadOnlyList<UnitType>? AttackerDeck { get; private set; }

    private static readonly JsonSerializerOptions DeckSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    static CmdArgs()
    {
        Parse();
        ReadEnvironmentVariables();
    }

    public static void Parse()
    {
        string[] engineArgs = OS.GetCmdlineArgs();
        string[] userArgs = OS.GetCmdlineUserArgs();


        IsServer = engineArgs.Contains("--server") || userArgs.Contains("--server");

        for (int i = 0; i < userArgs.Length; i++)
        {
            string arg = userArgs[i];

            if (arg.StartsWith("--email="))
            {
                Email = arg.Substring("--email=".Length);
            }
            else if (arg.StartsWith("--password="))
            {
                Password = arg.Substring("--password=".Length);
            }
            else if (arg.StartsWith("--port="))
            {
                SetPort(arg.Substring("--port=".Length));
            }
            else if (arg == "--port" && i + 1 < userArgs.Length)
            {
                i++;
                SetPort(userArgs[i]);
            }
            else if (arg.StartsWith("--match-id="))
            {
                MatchId = arg.Substring("--match-id=".Length);
            }
            else if (arg == "--match-id" && i + 1 < userArgs.Length)
            {
                i++;
                MatchId = userArgs[i];
            }
        }
    }

    private static void SetPort(string rawPort)
    {
        if (!int.TryParse(rawPort, out int parsedPort))
        {
            GD.PrintErr($"Invalid port argument '{rawPort}'. Expected an integer.");
            return;
        }

        if (parsedPort is < 1 or > 65535)
        {
            GD.PrintErr($"Port argument out of range '{parsedPort}'. Expected 1..65535.");
            return;
        }

        Port = parsedPort;
    }

    private static void ReadEnvironmentVariables()
    {
        // Only read on server mode
        if (!IsServer)
        {
            return;
        }

        // Read MATCH_ID from environment
        string? matchIdEnv = System.Environment.GetEnvironmentVariable("MATCH_ID");
        if (!string.IsNullOrWhiteSpace(matchIdEnv))
        {
            MatchId = matchIdEnv;
        }

        // Read DEFENDER_DECK_JSON from environment
        string? defenderDeckJson = System.Environment.GetEnvironmentVariable("DEFENDER_DECK_JSON");
        if (!string.IsNullOrWhiteSpace(defenderDeckJson))
        {
            try
            {
                DefenderDeck = JsonSerializer.Deserialize<List<UnitType>>(defenderDeckJson, DeckSerializerOptions);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to deserialize DEFENDER_DECK_JSON: {ex.Message}");
                DefenderDeck = null;
            }
        }

        // Read ATTACKER_DECK_JSON from environment
        string? attackerDeckJson = System.Environment.GetEnvironmentVariable("ATTACKER_DECK_JSON");
        if (!string.IsNullOrWhiteSpace(attackerDeckJson))
        {
            try
            {
                AttackerDeck = JsonSerializer.Deserialize<List<UnitType>>(attackerDeckJson, DeckSerializerOptions);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to deserialize ATTACKER_DECK_JSON: {ex.Message}");
                AttackerDeck = null;
            }
        }
    }
}
