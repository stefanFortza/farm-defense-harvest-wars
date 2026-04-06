using System.Text.Json;
using FarmDefenseHarvestWars.Backend.Models;
using Microsoft.AspNetCore.Identity;

namespace FarmDefenseHarvestWars.Backend.Services;

public sealed class DevelopmentTestUserSeeder
{
    private readonly IWebHostEnvironment _environment;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DevelopmentTestUserSeeder> _logger;

    public DevelopmentTestUserSeeder(
        IWebHostEnvironment environment,
        UserManager<ApplicationUser> userManager,
        ILogger<DevelopmentTestUserSeeder> logger)
    {
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
                continue;
            }

            failedCount++;
            string errors = string.Join("; ", result.Errors.Select(error => error.Description));
            _logger.LogWarning("Failed to seed user {Email}: {Errors}", userEntry.Email, errors);
        }

        _logger.LogInformation(
            "Development test-user seed completed. Created: {Created}, Skipped: {Skipped}, Failed: {Failed}",
            createdCount,
            skippedCount,
            failedCount);
    }

    private sealed class SeedUserEntry
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}