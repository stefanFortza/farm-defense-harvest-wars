using System.Threading.Tasks;
using NetHttp = System.Net.Http;
using System.Net.Http.Json;
using FarmDefenseHarvestWars.Shared.Models;

public static class GameClient
{
    private static readonly NetHttp.HttpClient _client = NetworkManager.HttpClient;

    public static async Task<PlayerProfileDto?> GetProfileAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new NetHttp.Headers.AuthenticationHeaderValue("Bearer", token);
        return await _client.GetFromJsonAsync<PlayerProfileDto>($"{ApiConfig.BaseUrl}/api/Game/profile");
    }
}
