using System.Text.Json.Serialization;

namespace FarmDefenseHarvestWars.Shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UnitType
{
    None,
    // Defenders
    Cow,
    Chicken,
    Sheep,
    Pig,

    // Attackers
    Wolf,
    Fox,
    Bear
}
