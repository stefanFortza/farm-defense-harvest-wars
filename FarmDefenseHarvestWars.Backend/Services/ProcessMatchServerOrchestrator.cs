using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;

namespace FarmDefenseHarvestWars.Backend.Services;

public sealed class ProcessMatchServerOrchestrator : IMatchServerOrchestrator
{
    private static int _nextPort;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProcessMatchServerOrchestrator> _logger;

    private static readonly JsonSerializerOptions DeckSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public ProcessMatchServerOrchestrator(
        IConfiguration configuration,
        ILogger<ProcessMatchServerOrchestrator> logger)
    {
        _configuration = configuration;
        _logger = logger;

        if (_nextPort == 0)
        {
            _nextPort = _configuration.GetValue<int?>("GodotServer:StartingPort") ?? 7777;
        }
    }

    public Task<MatchServerEndpoint> StartMatchServerAsync(
        string matchId,
        IReadOnlyCollection<UnitUnlockDto> defenderDeck,
        IReadOnlyCollection<UnitUnlockDto> attackerDeck,
        CancellationToken cancellationToken = default)
    {
        string executablePath = _configuration["GodotServer:ExecutablePath"] ?? string.Empty;
        string projectPath = _configuration["GodotServer:ProjectPath"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(executablePath) || string.IsNullOrWhiteSpace(projectPath))
        {
            throw new InvalidOperationException("GodotServer:ExecutablePath and GodotServer:ProjectPath must be configured.");
        }

        string host = _configuration["GodotServer:Host"] ?? "127.0.0.1";
        int port = Interlocked.Increment(ref _nextPort) - 1;

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };

        startInfo.ArgumentList.Add("--headless");
        startInfo.ArgumentList.Add("--path");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("--server");
        startInfo.ArgumentList.Add("--port");
        startInfo.ArgumentList.Add(port.ToString());
        startInfo.ArgumentList.Add("--match-id");
        startInfo.ArgumentList.Add(matchId);

        startInfo.Environment["MATCH_ID"] = matchId;
        startInfo.Environment["DEFENDER_DECK_JSON"] = JsonSerializer.Serialize(defenderDeck, DeckSerializerOptions);
        startInfo.Environment["ATTACKER_DECK_JSON"] = JsonSerializer.Serialize(attackerDeck, DeckSerializerOptions);
        startInfo.Environment["BACKEND_BASE_URL"] = _configuration["GodotServer:BackendBaseUrl"] ?? "http://localhost:5177";
        startInfo.Environment["MATCH_SERVER_CALLBACK_KEY"] = _configuration["GodotServer:CallbackKey"] ?? string.Empty;

        var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start Godot server process.");
        }

        _logger.LogInformation(
            "Started Godot server process {ProcessId} for match {MatchId} on {Host}:{Port}",
            process.Id,
            matchId,
            host,
            port);

        return Task.FromResult(new MatchServerEndpoint(host, port));
    }
}
