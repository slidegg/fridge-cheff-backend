using Microsoft.EntityFrameworkCore;
using RecipeApp.Api.Contracts.Requests;
using RecipeApp.Api.Contracts.Responses;
using RecipeApp.Api.Data;
using RecipeApp.Api.Data.Entities;
using RecipeApp.Api.Exceptions;

namespace RecipeApp.Api.Services;

public class DeviceService(AppDbContext db)
{
    public async Task<RegisterDeviceResponse> RegisterAsync(RegisterDeviceRequest req)
    {
        var existing = await db.DeviceUsers
            .FirstOrDefaultAsync(u => u.DeviceId == req.DeviceId);

        if (existing is not null)
        {
            existing.LastSeenAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return new RegisterDeviceResponse(existing.Id, existing.DeviceId);
        }

        var user = new DeviceUser
        {
            Id = Guid.NewGuid(),
            DeviceId = req.DeviceId,
            CreatedAtUtc = DateTime.UtcNow,
            LastSeenAtUtc = DateTime.UtcNow,
        };

        db.DeviceUsers.Add(user);
        await db.SaveChangesAsync();

        return new RegisterDeviceResponse(user.Id, user.DeviceId);
    }

    public async Task<DeviceUser> GetByDeviceIdAsync(string deviceId)
    {
        return await db.DeviceUsers.FirstOrDefaultAsync(u => u.DeviceId == deviceId)
               ?? throw new NotFoundException(
                   $"Device '{deviceId}' not registered. Call POST /api/devices/register first.");
    }
}
