using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;

namespace FarmDefenseHarvestWars.Backend.Services;

public interface IDeckService
{
    Task<DeckDto> GetDeckAsync(string userId, PlayerRole role, CancellationToken cancellationToken = default);
    DeckDto GetDefaultDeck(PlayerRole role);
    Task<DeckDto> UpdateDeckAsync(string userId, PlayerRole role, UpdateDeckDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnitType>> GetUnitCompositionAsync(string userId, PlayerRole role, CancellationToken cancellationToken = default);
}
