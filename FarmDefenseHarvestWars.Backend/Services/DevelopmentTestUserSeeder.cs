using System.Text.Json;
using FarmDefenseHarvestWars.Backend.Data;
using FarmDefenseHarvestWars.Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FarmDefenseHarvestWars.Backend.Services;

public sealed class DevelopmentTestUserSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly IDefaultUnitUnlockService _defaultUnitUnlockService;
    private readonly IWebHostEnvironment _environment;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DevelopmentTestUserSeeder> _logger;

    public DevelopmentTestUserSeeder(
        ApplicationDbContext db,
        IDefaultUnitUnlockService defaultUnitUnlockService,
        IWebHostEnvironment environment,
        UserManager<ApplicationUser> userManager,
        ILogger<DevelopmentTestUserSeeder> logger)
    {
        _db = db;
        _defaultUnitUnlockService = defaultUnitUnlockService;
        _environment = environment;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        string seedFilePath = Path.Combine(_environment.ContentRootPath, "user.txt");
        if (!File.Exists(seedFilePath))
        {
            _logger.LogWarning("Seed file {Path} not found. Skipping development test-user seed.", seedFilePath);
            return;
        }

        List<SeedUserEntry>? users;

        try
        {
            string json = await File.ReadAllTextAsync(seedFilePath, cancellationToken);
            users = JsonSerializer.Deserialize<List<SeedUserEntry>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse {Path}. Skipping development test-user seed.", seedFilePath);
            return;
        }

        if (users == null || users.Count == 0)
        {
            _logger.LogInformation("No users found in {Path}.", seedFilePath);
            return;
        }

        int createdCount = 0;
        int skippedCount = 0;
        int failedCount = 0;
        int defaultUnlocksCreatedCount = 0;

        foreach (SeedUserEntry userEntry in users)
        {
            if (string.IsNullOrWhiteSpace(userEntry.Email) || string.IsNullOrWhiteSpace(userEntry.Password))
            {
                skippedCount++;
                continue;
            }

            ApplicationUser? existing = await _userManager.FindByEmailAsync(userEntry.Email);
            if (existing != null)
            {
                skippedCount++;
                continue;
            }

            var user = new ApplicationUser
            {
                Email = userEntry.Email,
                UserName = userEntry.Email
            };

            IdentityResult result = await _userManager.CreateAsync(user, userEntry.Password);
            if (result.Succeeded)
            {
                createdCount++;

                IReadOnlyList<UnitUnlock> defaultUnlocks = _defaultUnitUnlockService
                    .CreateDefaultUnlocks(user.Id, DateTime.UtcNow);

                if (defaultUnlocks.Count > 0)
                {
                    HashSet<string> existingKeys = await _db.UnitUnlocks
                        .Where(unlock => unlock.UserId == user.Id)
                        .Select(unlock => $"{unlock.Role}:{unlock.UnitType}")
                        .ToHashSetAsync(cancellationToken);

                    List<UnitUnlock> missingUnlocks = defaultUnlocks
                        .Where(unlock => existingKeys.Add($"{unlock.Role}:{unlock.UnitType}"))
                        .ToList();

                    if (missingUnlocks.Count > 0)
                    {
                        await _db.UnitUnlocks.AddRangeAsync(missingUnlocks, cancellationToken);
                        await _db.SaveChangesAsync(cancellationToken);
                        defaultUnlocksCreatedCount += missingUnlocks.Count;
                    }
                }

                continue;
            }

            failedCount++;
            string errors = string.Join("; ", result.Errors.Select(error => error.Description));
            _logger.LogWarning("Failed to seed user {Email}: {Errors}", userEntry.Email, errors);
        }

        _logger.LogInformation(
            "Development test-user seed completed. Created: {Created}, Skipped: {Skipped}, Failed: {Failed}, DefaultUnlocksCreated: {DefaultUnlocksCreated}",
            createdCount,
            skippedCount,
            failedCount,
            defaultUnlocksCreatedCount);
    }

    private sealed class SeedUserEntry
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}