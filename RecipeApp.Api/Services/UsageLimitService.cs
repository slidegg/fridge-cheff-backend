using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RecipeApp.Api.Contracts.Responses;
using RecipeApp.Api.Data;
using RecipeApp.Api.Data.Entities;
using RecipeApp.Api.Exceptions;
using RecipeApp.Api.Options;

namespace RecipeApp.Api.Services;

public class UsageLimitService(AppDbContext db, IOptions<FreeTierOptions> options)
{
    /// <summary>Throws if the daily limit is already reached. Does not consume a unit — call IncrementAsync after the operation succeeds.</summary>
    public async Task CheckLimitAsync(Guid deviceUserId, LimitType limitType)
    {
        if (options.Value.UnlimitedUsage)
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var counter = await GetOrCreateCounterAsync(deviceUserId, today);
        await db.SaveChangesAsync();

        var (current, limit, label) = GetCurrentAndLimit(counter, limitType);

        if (current >= limit)
            throw new RateLimitException(
                $"Daily limit of {limit} {label} reached. Try again tomorrow.");
    }

    /// <summary>Call only after the corresponding operation has actually succeeded.</summary>
    public async Task IncrementAsync(Guid deviceUserId, LimitType limitType)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var counter = await GetOrCreateCounterAsync(deviceUserId, today);

        switch (limitType)
        {
            case LimitType.Scan: counter.ScansUsed++; break;
            case LimitType.RecipeSearch: counter.RecipeSearchesUsed++; break;
            case LimitType.RecipeDetail: counter.RecipeDetailsUsed++; break;
        }

        counter.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private (int Current, int Limit, string Label) GetCurrentAndLimit(
        UsageCounter counter, LimitType limitType) => limitType switch
    {
        LimitType.Scan => (counter.ScansUsed, options.Value.DailyScanLimit, "scans"),
        LimitType.RecipeSearch => (counter.RecipeSearchesUsed, options.Value.DailyRecipeSearchLimit, "recipe searches"),
        LimitType.RecipeDetail => (counter.RecipeDetailsUsed, options.Value.DailyRecipeDetailLimit, "recipe detail views"),
        _ => throw new ArgumentException($"Unknown limit type: {limitType}")
    };

    public async Task<UsageResponse> GetUsageAsync(Guid deviceUserId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var counter = await GetOrCreateCounterAsync(deviceUserId, today);
        await db.SaveChangesAsync();

        return new UsageResponse(
            DateUtc: today.ToString("yyyy-MM-dd"),
            ScansUsed: counter.ScansUsed,
            ScanLimit: options.Value.DailyScanLimit,
            RecipeSearchesUsed: counter.RecipeSearchesUsed,
            RecipeSearchLimit: options.Value.DailyRecipeSearchLimit,
            RecipeDetailsUsed: counter.RecipeDetailsUsed,
            RecipeDetailLimit: options.Value.DailyRecipeDetailLimit,
            IsUnlimited: options.Value.UnlimitedUsage
        );
    }

    private async Task<UsageCounter> GetOrCreateCounterAsync(Guid deviceUserId, DateOnly date)
    {
        var counter = await db.UsageCounters
            .FirstOrDefaultAsync(c => c.DeviceUserId == deviceUserId && c.DateUtc == date);

        if (counter is not null)
            return counter;

        counter = new UsageCounter
        {
            Id = Guid.NewGuid(),
            DeviceUserId = deviceUserId,
            DateUtc = date,
            ScansUsed = 0,
            RecipeSearchesUsed = 0,
            RecipeDetailsUsed = 0,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        db.UsageCounters.Add(counter);
        return counter;
    }
}
