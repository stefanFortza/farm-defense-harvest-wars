using System.Text.Json.Serialization;

namespace FarmDefenseHarvestWars.Shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlayerRole
{
    Defender,
    Attacker
}
