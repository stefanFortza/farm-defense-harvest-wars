using FarmDefenseHarvestWars.Backend.Data;
using FarmDefenseHarvestWars.Backend.Models;
using FarmDefenseHarvestWars.Backend.Services;
using FarmDefenseHarvestWars.Shared.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace FarmDefenseHarvestWars.Backend.Tests.Services;

public class DefaultUnitUnlockServiceTests
{
    [Fact]
    public async Task EnsureDefaultUnlocksAsync_ShouldOnlyAddDefaultUnlockedUnits()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;

        using var context = new ApplicationDbContext(options);
        var mockEnvironment = Substitute.For<IWebHostEnvironment>();
        mockEnvironment.ContentRootPath.Returns(Path.GetFullPath("../../../../FarmDefenseHarvestWars.Backend"));
        
        var registryProvider = new UnitRegistryProvider(mockEnvironment);
        var service = new DefaultUnitUnlockService(context, registryProvider);

        var userId = "test-user-1";

        // Act
        await service.EnsureDefaultUnlocksAsync(userId, CancellationToken.None);

        // Assert
        var unlocks = await context.UnitUnlocks.Where(u => u.UserId == userId).ToListAsync();
        
        // Duck should NOT be unlocked
        Assert.DoesNotContain(unlocks, u => u.UnitType == UnitType.Duck);
        
        // Cow SHOULD be unlocked
        Assert.Contains(unlocks, u => u.UnitType == UnitType.Cow);
        
        // Let's count how many are unlocked
        var expectedCount = registryProvider.GetAllUnits().Count(u => u.IsDefaultUnlocked);
        Assert.Equal(expectedCount, unlocks.Count);
    }
}
