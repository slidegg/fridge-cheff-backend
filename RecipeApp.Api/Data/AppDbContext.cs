using Microsoft.EntityFrameworkCore;
using RecipeApp.Api.Data.Entities;

namespace RecipeApp.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DeviceUser> DeviceUsers => Set<DeviceUser>();
    public DbSet<FridgeScan> FridgeScans => Set<FridgeScan>();
    public DbSet<ScanImage> ScanImages => Set<ScanImage>();
    public DbSet<DetectedIngredient> DetectedIngredients => Set<DetectedIngredient>();
    public DbSet<ConfirmedIngredient> ConfirmedIngredients => Set<ConfirmedIngredient>();
    public DbSet<RecipeSearch> RecipeSearches => Set<RecipeSearch>();
    public DbSet<RecipeInteraction> RecipeInteractions => Set<RecipeInteraction>();
    public DbSet<SavedRecipe> SavedRecipes => Set<SavedRecipe>();
    public DbSet<UsageCounter> UsageCounters => Set<UsageCounter>();
    public DbSet<ExternalApiCallLog> ExternalApiCallLogs => Set<ExternalApiCallLog>();
    public DbSet<DeviceSettings> DeviceSettings => Set<DeviceSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<DeviceUser>(e =>
        {
            e.HasIndex(u => u.DeviceId).IsUnique();
        });

        builder.Entity<UsageCounter>(e =>
        {
            e.HasIndex(u => new { u.DeviceUserId, u.DateUtc }).IsUnique();
        });

        builder.Entity<DeviceSettings>(e =>
        {
            e.HasIndex(s => s.DeviceUserId).IsUnique();
        });

        // Store enums as strings for readability in the database
        builder.Entity<FridgeScan>()
            .Property(s => s.Status)
            .HasConversion<string>();

        builder.Entity<RecipeSearch>()
            .Property(s => s.Goal)
            .HasConversion<string>();

        builder.Entity<RecipeInteraction>()
            .Property(i => i.Action)
            .HasConversion<string>();

        builder.Entity<ExternalApiCallLog>()
            .Property(l => l.Provider)
            .HasConversion<string>();
    }
}
