using FarmDefenseHarvestWars.Backend.Services;
using FarmDefenseHarvestWars.Shared.Enums;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;
using Xunit;

namespace FarmDefenseHarvestWars.Backend.Tests.Services;

public class UnitRegistryProviderTests
{
    [Fact]
    public void GetDefaultUnlockedUnitsForRole_ShouldOnlyReturnDefaultUnlockedUnits()
    {
        // Arrange
        var mockEnvironment = Substitute.For<IWebHostEnvironment>();
        // We need to point to the actual Data directory or a mock one.
        // For simplicity, let's assume we can point to the one in the Backend project.
        mockEnvironment.ContentRootPath.Returns(Path.GetFullPath("../../../../FarmDefenseHarvestWars.Backend"));
        
        var provider = new UnitRegistryProvider(mockEnvironment);

        // Act
        var defenderUnits = provider.GetDefaultUnlockedUnitsForRole(PlayerRole.Defender);
        var attackerUnits = provider.GetDefaultUnlockedUnitsForRole(PlayerRole.Attacker);

        // Assert
        // Duck has IsDefaultUnlocked: false in UnitRegistry.json
        Assert.DoesNotContain(UnitType.Duck, defenderUnits);
        Assert.DoesNotContain(UnitType.Duck, attackerUnits);
        
        // Cow has IsDefaultUnlocked: true in UnitRegistry.json
        Assert.Contains(UnitType.Cow, defenderUnits);
    }
}
