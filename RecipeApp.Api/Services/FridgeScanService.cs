using Microsoft.EntityFrameworkCore;
using RecipeApp.Api.Contracts.Requests;
using RecipeApp.Api.Contracts.Responses;
using RecipeApp.Api.Data;
using RecipeApp.Api.Data.Entities;
using RecipeApp.Api.Exceptions;
using RecipeApp.Api.Services.OpenAI;

namespace RecipeApp.Api.Services;

public class FridgeScanService(
    AppDbContext db,
    DeviceService deviceService,
    IOpenAiVisionService visionService,
    UsageLimitService usageLimitService,
    ImageStorageService imageStorageService,
    IngredientNormalizationService normalizationService,
    ILogger<FridgeScanService> logger)
{
    public async Task<FridgeScanResponse> CreateScanAsync(string deviceId, IFormFileCollection images)
    {
        var deviceUser = await deviceService.GetByDeviceIdAsync(deviceId);

        await usageLimitService.CheckLimitAsync(deviceUser.Id, LimitType.Scan);

        imageStorageService.ValidateImages(images);

        var scan = new FridgeScan
        {
            Id = Guid.NewGuid(),
            DeviceUserId = deviceUser.Id,
            CreatedAtUtc = DateTime.UtcNow,
            ImageCount = images.Count,
            Status = ScanStatus.Pending,
        };
        db.FridgeScans.Add(scan);
        await db.SaveChangesAsync();

        var savedImages = await imageStorageService.SaveImagesAsync(scan.Id, images);
        db.ScanImages.AddRange(savedImages);

        OpenAiVisionResult visionResult;
        try
        {
            visionResult = await visionService.DetectIngredientsAsync(images);
            scan.Status = ScanStatus.Processed;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Vision detection failed for scan {ScanId}", scan.Id);
            scan.Status = ScanStatus.Failed;
            scan.ErrorMessage = ex.Message;
            await db.SaveChangesAsync();
            throw new AppValidationException("Ingredient detection failed. Please try again.");
        }

        await usageLimitService.IncrementAsync(deviceUser.Id, LimitType.Scan);

        var detectedIngredients = visionResult.Items.Select(item => new DetectedIngredient
        {
            Id = Guid.NewGuid(),
            FridgeScanId = scan.Id,
            Name = item.Name,
            NormalizedName = normalizationService.Normalize(item.Name),
            Category = item.Category,
            Confidence = item.Confidence,
            QuantityVisible = item.QuantityVisible,
            NeedsConfirmation = item.NeedsConfirmation,
            SourceImageIndex = item.SourceImageIndex,
            CreatedAtUtc = DateTime.UtcNow,
        }).ToList();

        db.DetectedIngredients.AddRange(detectedIngredients);
        await db.SaveChangesAsync();

        logger.LogInformation("Scan {ScanId}: {Count} ingredients detected", scan.Id, detectedIngredients.Count);

        return new FridgeScanResponse(
            ScanId: scan.Id,
            DetectedItems: detectedIngredients.Select(i => new DetectedIngredientDto(
                i.Name, i.NormalizedName, i.Category, i.Confidence, i.QuantityVisible, i.NeedsConfirmation
            )).ToList(),
            Warnings: visionResult.Warnings
        );
    }

    /// <summary>Creates an empty, already-processed scan shell so a user can build their
    /// ingredient list manually via the same confirm-ingredients flow as a photo scan.
    /// Does not consume the daily scan limit — no AI call is made.</summary>
    public async Task<FridgeScanResponse> CreateManualScanAsync(string deviceId)
    {
        var deviceUser = await deviceService.GetByDeviceIdAsync(deviceId);

        var scan = new FridgeScan
        {
            Id = Guid.NewGuid(),
            DeviceUserId = deviceUser.Id,
            CreatedAtUtc = DateTime.UtcNow,
            ImageCount = 0,
            Status = ScanStatus.Processed,
        };
        db.FridgeScans.Add(scan);
        await db.SaveChangesAsync();

        logger.LogInformation("Manual scan {ScanId} created for device {DeviceId}", scan.Id, deviceId);

        return new FridgeScanResponse(
            ScanId: scan.Id,
            DetectedItems: [],
            Warnings: []
        );
    }

    public async Task<ConfirmIngredientsResponse> ConfirmIngredientsAsync(
        Guid scanId, ConfirmIngredientsRequest req)
    {
        var scan = await db.FridgeScans.FindAsync(scanId)
                   ?? throw new NotFoundException($"Scan '{scanId}' not found.");

        if (scan.Status != ScanStatus.Processed)
            throw new AppValidationException("Scan has not been successfully processed.");

        // Replace any previous confirmations
        var existing = db.ConfirmedIngredients.Where(c => c.FridgeScanId == scanId);
        db.ConfirmedIngredients.RemoveRange(existing);

        var normalized = normalizationService.NormalizeList(req.Ingredients);

        var confirmed = normalized.Select(name => new ConfirmedIngredient
        {
            Id = Guid.NewGuid(),
            FridgeScanId = scanId,
            Name = name,
            NormalizedName = normalizationService.Normalize(name),
            CreatedAtUtc = DateTime.UtcNow,
        }).ToList();

        db.ConfirmedIngredients.AddRange(confirmed);
        await db.SaveChangesAsync();

        return new ConfirmIngredientsResponse(scanId, confirmed.Select(c => c.Name).ToList());
    }
}
