using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FarmDefenseHarvestWars.Shared.Enums;

using FarmDefenseHarvestWars.Shared.Models.Game;

namespace FarmDefenseHarvestWars.GameClient.Scripts.Utils;

public static class CmdArgs
{
    public static string? Email { get; private set; }
    public static string? Password { get; private set; }
    public static bool IsServer { get; private set; }
    public static int? Port { get; private set; }
    public static string? MatchId { get; private set; }
    public static string? BackendBaseUrl { get; private set; }
    public static string? MatchServerCallbackKey { get; private set; }
    public static IReadOnlyList<UnitUnlockDto>? DefenderDeck { get; private set; }
    public static IReadOnlyList<UnitUnlockDto>? AttackerDeck { get; private set; }
    public static int DefenderAvatarIndex { get; private set; } = 1;
    public static int AttackerAvatarIndex { get; private set; } = 1;
    public static string DefenderName { get; private set; } = "Defender";
    public static string AttackerName { get; private set; } = "Attacker";

    private static readonly JsonSerializerOptions DeckSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    static CmdArgs()
    {
        Parse();
        ReadEnvironmentVariables();
        ReadConfigFile();
    }

    public static void Parse()
    {
        string[] engineArgs = OS.GetCmdlineArgs();
        string[] userArgs = OS.GetCmdlineUserArgs();

        GD.Print($"[CmdArgs] Engine args: {string.Join(" ", engineArgs)}");
        GD.Print($"[CmdArgs] User args: {string.Join(" ", userArgs)}");

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
            else if (arg.StartsWith("--backend-url="))
            {
                BackendBaseUrl = arg.Substring("--backend-url=".Length);
            }
            else if (arg == "--backend-url" && i + 1 < userArgs.Length)
            {
                i++;
                BackendBaseUrl = userArgs[i];
            }
            else if (arg.StartsWith("--callback-key="))
            {
                MatchServerCallbackKey = arg.Substring("--callback-key=".Length);
            }
            else if (arg == "--callback-key" && i + 1 < userArgs.Length)
            {
                i++;
                MatchServerCallbackKey = userArgs[i];
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

    private static void ReadConfigFile()
    {
        // Search order: 
        // 1. res:// (Internal pck, if included via export filters)
        // 2. user:// (AppData/local folders)
        // 3. Executable directory (Portable/Exported external config)
        var configPaths = new List<string> { "res://config.cfg", "user://config.cfg" };

        if (OS.HasFeature("standalone"))
        {
            configPaths.Add(OS.GetExecutablePath().GetBaseDir().PathJoin("config.cfg"));
        }

        var config = new ConfigFile();

        foreach (var path in configPaths)
        {
            if (FileAccess.FileExists(path))
            {
                Error err = config.Load(path);
                if (err != Error.Ok)
                {
                    GD.PrintErr($"[CmdArgs] Failed to load config file at {path}: {err}");
                    continue;
                }

                GD.Print($"[CmdArgs] Loading config from {path}");

                // Prioritize existing values (cmd args / env vars) over config file
                BackendBaseUrl ??= (string)config.GetValue("Network", "backend_url", BackendBaseUrl);
                Email ??= (string)config.GetValue("Auth", "email", Email);
                Password ??= (string)config.GetValue("Auth", "password", Password);

                if (Port == null)
                {
                    Variant portVar = config.GetValue("Server", "port");
                    if (portVar.VariantType != Variant.Type.Nil)
                    {
                        Port = (int)portVar;
                    }
                }
            }
        }
    }

    private static void ReadEnvironmentVariables()
    {
        // Read values from environment (higher priority than config file, lower than cmd args)

        string? backendUrlEnv = System.Environment.GetEnvironmentVariable("BACKEND_BASE_URL");
        if (!string.IsNullOrWhiteSpace(backendUrlEnv))
        {
            BackendBaseUrl = backendUrlEnv;
        }

        string? emailEnv = System.Environment.GetEnvironmentVariable("GAME_EMAIL");
        if (!string.IsNullOrWhiteSpace(emailEnv))
        {
            Email = emailEnv;
        }

        string? passwordEnv = System.Environment.GetEnvironmentVariable("GAME_PASSWORD");
        if (!string.IsNullOrWhiteSpace(passwordEnv))
        {
            Password = passwordEnv;
        }

        // Only read server-specific values on server mode
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

        string? callbackKeyEnv = System.Environment.GetEnvironmentVariable("MATCH_SERVER_CALLBACK_KEY");
        if (!string.IsNullOrWhiteSpace(callbackKeyEnv))
        {
            MatchServerCallbackKey = callbackKeyEnv;
        }

        string? defAvatarEnv = System.Environment.GetEnvironmentVariable("DEFENDER_AVATAR_INDEX");
        if (int.TryParse(defAvatarEnv, out int defAvatar))
        {
            DefenderAvatarIndex = defAvatar;
        }

        string? atkAvatarEnv = System.Environment.GetEnvironmentVariable("ATTACKER_AVATAR_INDEX");
        if (int.TryParse(atkAvatarEnv, out int atkAvatar))
        {
            AttackerAvatarIndex = atkAvatar;
        }

        DefenderName = System.Environment.GetEnvironmentVariable("DEFENDER_NAME") ?? "Defender";
        AttackerName = System.Environment.GetEnvironmentVariable("ATTACKER_NAME") ?? "Attacker";

        // Read DEFENDER_DECK_JSON from environment
        string? defenderDeckJson = System.Environment.GetEnvironmentVariable("DEFENDER_DECK_JSON");
        if (!string.IsNullOrWhiteSpace(defenderDeckJson))
        {
            try
            {
                DefenderDeck = JsonSerializer.Deserialize<List<UnitUnlockDto>>(defenderDeckJson, DeckSerializerOptions);
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
                AttackerDeck = JsonSerializer.Deserialize<List<UnitUnlockDto>>(attackerDeckJson, DeckSerializerOptions);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to deserialize ATTACKER_DECK_JSON: {ex.Message}");
                AttackerDeck = null;
            }
        }
    }
}
