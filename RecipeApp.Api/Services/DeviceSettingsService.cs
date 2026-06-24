using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RecipeApp.Api.Contracts.Requests;
using RecipeApp.Api.Contracts.Responses;
using RecipeApp.Api.Data;
using RecipeApp.Api.Data.Entities;

namespace RecipeApp.Api.Services;

public class DeviceSettingsService(AppDbContext db)
{
    public async Task<DeviceSettingsResponse> GetSettingsAsync(Guid deviceUserId)
    {
        var settings = await GetOrCreateEntityAsync(deviceUserId);
        return Map(settings);
    }

    /// <summary>Returns the raw entity for internal use by other services (e.g. RecipeService).</summary>
    public async Task<DeviceSettings> GetOrCreateEntityAsync(Guid deviceUserId)
    {
        var settings = await db.DeviceSettings
            .FirstOrDefaultAsync(s => s.DeviceUserId == deviceUserId);

        if (settings is not null)
            return settings;

        settings = new DeviceSettings
        {
            Id = Guid.NewGuid(),
            DeviceUserId = deviceUserId,
            AllowMissingIngredients = false,
            MaxMissingIngredients = 0,
            IgnorePantry = true,
            AlwaysAvailableIngredientsJson = JsonSerializer.Serialize(new List<string>()),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        db.DeviceSettings.Add(settings);
        await db.SaveChangesAsync();
        return settings;
    }

    public async Task<DeviceSettingsResponse> UpdateSettingsAsync(Guid deviceUserId, UpdateDeviceSettingsRequest req)
    {
        var settings = await GetOrCreateEntityAsync(deviceUserId);

        var normalizedIngredients = req.AlwaysAvailableIngredients
            .Select(p => p.Trim().ToLowerInvariant())
            .Where(p => p.Length > 0)
            .Distinct()
            .ToList();

        settings.AllowMissingIngredients = req.AllowMissingIngredients;
        settings.MaxMissingIngredients = Math.Clamp(req.MaxMissingIngredients, 0, 10);
        settings.IgnorePantry = req.IgnorePantry;
        settings.AlwaysAvailableIngredientsJson = JsonSerializer.Serialize(normalizedIngredients);
        settings.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Map(settings);
    }

    public static List<string> GetAlwaysAvailableIngredients(DeviceSettings settings) =>
        JsonSerializer.Deserialize<List<string>>(settings.AlwaysAvailableIngredientsJson) ?? [];

    private static DeviceSettingsResponse Map(DeviceSettings s) => new(
        AllowMissingIngredients: s.AllowMissingIngredients,
        MaxMissingIngredients: s.MaxMissingIngredients,
        IgnorePantry: s.IgnorePantry,
        AlwaysAvailableIngredients: GetAlwaysAvailableIngredients(s)
    );
}
