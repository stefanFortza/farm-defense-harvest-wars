
using System.Threading.Tasks;
using System.Net.Http.Json;
using NetHttp = System.Net.Http;
using FarmDefenseHarvestWars.Shared.Models;

public static class AuthClient
{
    private static readonly NetHttp.HttpClient _client = NetworkManager.HttpClient;

    public static async Task<string?> LoginAsync(string email, string password)
    {
        var loginData = new { email, password };
        var res = await _client.PostAsJsonAsync($"{ApiConfig.BaseUrl}/login", loginData);
        if (!res.IsSuccessStatusCode) return null;
        var json = await res.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("accessToken").GetString();
    }
}
