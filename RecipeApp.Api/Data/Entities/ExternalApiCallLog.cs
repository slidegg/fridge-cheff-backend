namespace RecipeApp.Api.Data.Entities;

public class ExternalApiCallLog
{
    public Guid Id { get; set; }
    public Guid? DeviceUserId { get; set; }
    public ApiProvider Provider { get; set; }
    public string Endpoint { get; set; } = default!;
    public string RequestType { get; set; } = default!;
    public int? StatusCode { get; set; }
    public bool Success { get; set; }
    public decimal? CostUnits { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public long DurationMs { get; set; }
}
