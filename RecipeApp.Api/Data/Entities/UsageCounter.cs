namespace RecipeApp.Api.Data.Entities;

public class UsageCounter
{
    public Guid Id { get; set; }
    public Guid DeviceUserId { get; set; }
    public DateOnly DateUtc { get; set; }
    public int ScansUsed { get; set; }
    public int RecipeSearchesUsed { get; set; }
    public int RecipeDetailsUsed { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public DeviceUser DeviceUser { get; set; } = default!;
}
