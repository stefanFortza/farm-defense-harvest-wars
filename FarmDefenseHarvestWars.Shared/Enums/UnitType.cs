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
    Duck,

    // Attackers
    Skeleton,
    GoblinSpearman,
    Orc,
    SkeletonMage,
    Spearman,
}
