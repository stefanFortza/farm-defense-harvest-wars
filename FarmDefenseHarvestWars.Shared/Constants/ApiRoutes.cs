namespace FarmDefenseHarvestWars.Shared.Constants;

public static class ApiRoutes
{
    public const string Login = "/login";
    public const string Register = "/register";
    public const string Profile = "/api/game/profile";
    public const string DeckByRole = "/api/game/deck/{role}";
    public const string DefaultDeckByRole = "/api/game/deck/{role}/default";
    public const string UnlockUnit = "/api/game/unit/{unitType}/unlock";
    public const string MatchmakingQueue = "/api/game/matchmaking/queue";
    public const string MatchmakingStatus = "/api/game/matchmaking/status";
}
