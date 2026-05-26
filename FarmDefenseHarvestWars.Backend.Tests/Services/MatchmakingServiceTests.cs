using FarmDefenseHarvestWars.Backend.Models;
using FarmDefenseHarvestWars.Backend.Services;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FarmDefenseHarvestWars.Backend.Tests.Services;

public class MatchmakingServiceTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMatchServerOrchestrator _matchServerOrchestrator;
    private readonly ILogger<MatchmakingService> _logger;
    private readonly IDeckService _deckService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MatchmakingService _sut;

    public MatchmakingServiceTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _matchServerOrchestrator = Substitute.For<IMatchServerOrchestrator>();
        _logger = Substitute.For<ILogger<MatchmakingService>>();
        _deckService = Substitute.For<IDeckService>();
        
        // UserManager is notoriously hard to mock. We'll mock the IUserStore instead.
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(store, null, null, null, null, null, null, null, null);

        var scope = Substitute.For<IServiceScope>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(_serviceProvider);

        _serviceProvider.GetService(typeof(IDeckService)).Returns(_deckService);
        _serviceProvider.GetService(typeof(UserManager<ApplicationUser>)).Returns(_userManager);

        // Default mock behaviors
        _matchServerOrchestrator.StartMatchServerAsync(
            Arg.Any<string>(), Arg.Any<IReadOnlyCollection<UnitUnlockDto>>(), Arg.Any<IReadOnlyCollection<UnitUnlockDto>>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new MatchServerEndpoint("localhost", 1234));

        _deckService.GetUnitCompositionAsync(Arg.Any<string>(), Arg.Any<PlayerRole>(), Arg.Any<CancellationToken>())
            .Returns(new List<UnitUnlockDto>());

        _userManager.FindByIdAsync(Arg.Any<string>()).Returns(new ApplicationUser { UserName = "TestUser" });

        _sut = new MatchmakingService(_serviceProvider, _matchServerOrchestrator, _logger);
    }

    [Fact]
    public async Task MassMatchmaking_ShouldBeThreadSafe()
    {
        // Arrange
        const int attackersCount = 500;
        const int defendersCount = 500;
        
        var attackerIds = Enumerable.Range(0, attackersCount).Select(i => $"attacker_{i}").ToList();
        var defenderIds = Enumerable.Range(0, defendersCount).Select(i => $"defender_{i}").ToList();

        // Act
        // Mix them up to increase chance of race conditions
        var allRequests = attackerIds.Select(id => (id, role: PlayerRole.Attacker))
            .Concat(defenderIds.Select(id => (id, role: PlayerRole.Defender)))
            .OrderBy(_ => Guid.NewGuid())
            .ToList();

        var tasks = allRequests.Select(req => _sut.QueueForMatchAsync(req.id, req.role)).ToList();

        await Task.WhenAll(tasks);

        // Assert
        // All players should have found a match
        foreach (var id in attackerIds.Concat(defenderIds))
        {
            var status = _sut.GetStatusForUser(id);
            status.MatchFound.Should().BeTrue($"User {id} should have found a match");
        }

        // Orchestrator should be called exactly 500 times
        await _matchServerOrchestrator.Received(500).StartMatchServerAsync(
            Arg.Any<string>(), Arg.Any<IReadOnlyCollection<UnitUnlockDto>>(), Arg.Any<IReadOnlyCollection<UnitUnlockDto>>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueueForMatchAsync_ShouldBeIdempotent()
    {
        // Arrange
        var userId = "user1";

        // Act
        await _sut.QueueForMatchAsync(userId, PlayerRole.Attacker);
        await _sut.QueueForMatchAsync(userId, PlayerRole.Attacker);

        // Assert
        var status = _sut.GetStatusForUser(userId);
        status.IsQueued.Should().BeTrue();
        
        // If we add a defender now, it should match with this one user only once
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns(new ApplicationUser { UserName = "TestUser" });
        await _sut.QueueForMatchAsync("defender1", PlayerRole.Defender);

        await _matchServerOrchestrator.Received(1).StartMatchServerAsync(
            Arg.Any<string>(), Arg.Any<IReadOnlyCollection<UnitUnlockDto>>(), Arg.Any<IReadOnlyCollection<UnitUnlockDto>>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelMatchmaking_ShouldCleanUpState()
    {
        // Arrange
        var userId = "user1";
        await _sut.QueueForMatchAsync(userId, PlayerRole.Attacker);

        // Act
        _sut.CancelMatchmaking(userId);

        // Assert
        var status = _sut.GetStatusForUser(userId);
        status.IsQueued.Should().BeFalse();
        status.MatchFound.Should().BeFalse();
    }
}
