using FarmDefenseHarvestWars.Backend.Data;
using FarmDefenseHarvestWars.Backend.Models;
using FarmDefenseHarvestWars.Backend.Services;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace FarmDefenseHarvestWars.Backend.Tests.Services;

public class DeckServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IUnitRegistryProvider _unitRegistryProvider;
    private readonly IProfileService _profileService;
    private readonly DeckService _sut;
    private const string UserId = "test-user";

    public DeckServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        _unitRegistryProvider = Substitute.For<IUnitRegistryProvider>();
        _profileService = Substitute.For<IProfileService>();

        _sut = new DeckService(_db, _unitRegistryProvider, _profileService);
    }

    [Fact]
    public async Task UpdateDeckAsync_WithValidRequest_ShouldUpdateDeck()
    {
        // Arrange
        var role = PlayerRole.Attacker;
        var request = new UpdateDeckDto
        {
            Name = "New Deck Name",
            Units = new List<UnitType> { UnitType.Skeleton, UnitType.GoblinSpearman }
        };

        _unitRegistryProvider.UnitExists(Arg.Any<UnitType>()).Returns(true);
        _unitRegistryProvider.IsRoleCompatible(Arg.Any<UnitType>(), role).Returns(true);
        _profileService.GetUnlockedUnitTypesForRoleAsync(UserId, role, Arg.Any<CancellationToken>())
            .Returns(new HashSet<UnitType> { UnitType.Skeleton, UnitType.GoblinSpearman });

        // Act
        var result = await _sut.UpdateDeckAsync(UserId, role, request);

        // Assert
        result.Name.Should().Be("New Deck Name");
        result.Units.Should().HaveCount(2);
        result.Units.Should().Contain(new[] { UnitType.Skeleton, UnitType.GoblinSpearman });

        var deckInDb = await _db.Decks.FirstOrDefaultAsync(d => d.UserId == UserId && d.Role == role);
        deckInDb.Should().NotBeNull();
        deckInDb!.Name.Should().Be("New Deck Name");
    }

    [Fact]
    public async Task UpdateDeckAsync_WithTooManyUnits_ShouldThrowArgumentException()
    {
        // Arrange
        var role = PlayerRole.Attacker;
        var request = new UpdateDeckDto
        {
            Units = Enumerable.Range(0, 7).Select(_ => UnitType.Skeleton).ToList()
        };

        // Act & Assert
        var act = () => _sut.UpdateDeckAsync(UserId, role, request);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Deck can contain at most 6 units.");
    }

    [Fact]
    public async Task UpdateDeckAsync_WithDuplicateUnits_ShouldThrowArgumentException()
    {
        // Arrange
        var role = PlayerRole.Attacker;
        var request = new UpdateDeckDto
        {
            Units = new List<UnitType> { UnitType.Skeleton, UnitType.Skeleton }
        };

        // Act & Assert
        var act = () => _sut.UpdateDeckAsync(UserId, role, request);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Deck cannot contain duplicate units.");
    }

    [Fact]
    public async Task UpdateDeckAsync_WithLockedUnits_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var role = PlayerRole.Attacker;
        var request = new UpdateDeckDto
        {
            Units = new List<UnitType> { UnitType.Skeleton }
        };

        _unitRegistryProvider.UnitExists(UnitType.Skeleton).Returns(true);
        _unitRegistryProvider.IsRoleCompatible(UnitType.Skeleton, role).Returns(true);
        _profileService.GetUnlockedUnitTypesForRoleAsync(UserId, role, Arg.Any<CancellationToken>())
            .Returns(new HashSet<UnitType>()); // No units unlocked

        // Act & Assert
        var act = () => _sut.UpdateDeckAsync(UserId, role, request);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not unlocked*");
    }

    [Fact]
    public async Task UpdateDeckAsync_WithIncompatibleRole_ShouldThrowArgumentException()
    {
        // Arrange
        var role = PlayerRole.Defender;
        var request = new UpdateDeckDto
        {
            Units = new List<UnitType> { UnitType.Skeleton }
        };

        _unitRegistryProvider.UnitExists(UnitType.Skeleton).Returns(true);
        _unitRegistryProvider.IsRoleCompatible(UnitType.Skeleton, role).Returns(false);

        // Act & Assert
        var act = () => _sut.UpdateDeckAsync(UserId, role, request);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not valid for role*");
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
