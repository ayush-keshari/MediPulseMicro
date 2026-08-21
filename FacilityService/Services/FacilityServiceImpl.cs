using FacilityService.Data;
using FacilityService.DTOs;
using FacilityService.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityService.Services;

public class FacilityServiceImpl : IFacilityService
{
    private readonly FacilityDbContext _db;

    public FacilityServiceImpl(FacilityDbContext db) => _db = db;

    // ── Facilities ──────────────────────────────────────────────────────────

    public async Task<IEnumerable<FacilityDto>> GetAllFacilitiesAsync()
    {
        return await _db.Facilities
            .OrderByDescending(f => f.FacilityId)
            .Select(f => ToFacilityDto(f))
            .ToListAsync();
    }

    public async Task<FacilityDto?> GetFacilityByIdAsync(int id)
    {
        var facility = await _db.Facilities.FindAsync(id);
        return facility == null ? null : ToFacilityDto(facility);
    }

    public async Task<bool> CreateFacilityAsync(CreateFacilityRequest request)
    {
        if (await _db.Facilities.AnyAsync(f =>
                f.Name.ToLower() == request.Name.ToLower() &&
                f.Type           == request.Type &&
                f.Region         == request.Region))
            throw new InvalidOperationException(
                $"A facility named '{request.Name}' of type '{request.Type}' in region '{request.Region}' already exists.");

        var facility = new Facility
        {
            Name   = request.Name,
            Type   = request.Type,
            Region = request.Region
        };

        _db.Facilities.Add(facility);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateFacilityAsync(int id, UpdateFacilityRequest request)
    {
        var facility = await _db.Facilities.FindAsync(id);
        if (facility == null) return false;

        if (await _db.Facilities.AnyAsync(f =>
                f.FacilityId     != id &&
                f.Name.ToLower() == request.Name.ToLower() &&
                f.Type           == request.Type &&
                f.Region         == request.Region))
            throw new InvalidOperationException(
                $"A facility named '{request.Name}' of type '{request.Type}' in region '{request.Region}' already exists.");

        facility.Name   = request.Name;
        facility.Type   = request.Type;
        facility.Region = request.Region;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteFacilityAsync(int id)
    {
        // Load the facility with all its zones — same cascade logic as the monolith.
        var facility = await _db.Facilities
            .Include(f => f.StorageZones)
            .FirstOrDefaultAsync(f => f.FacilityId == id);

        if (facility == null) return false;

        // Delete all zones that belong to this facility first, then delete the facility.
        _db.StorageZones.RemoveRange(facility.StorageZones);
        _db.Facilities.Remove(facility);

        await _db.SaveChangesAsync();
        return true;
    }

    // ── StorageZones ────────────────────────────────────────────────────────

    public async Task<IEnumerable<StorageZoneDto>> GetAllZonesAsync()
    {
        return await _db.StorageZones
            .OrderByDescending(z => z.ZoneId)
            .Include(z => z.Facility)
            .Select(z => ToZoneDto(z))
            .ToListAsync();
    }

    public async Task<IEnumerable<StorageZoneDto>> GetZonesByFacilityAsync(int facilityId)
    {
        return await _db.StorageZones
            .Where(z => z.FacilityId == facilityId)
            .OrderByDescending(z => z.ZoneId)
            .Include(z => z.Facility)
            .Select(z => ToZoneDto(z))
            .ToListAsync();
    }

    public async Task<StorageZoneDto?> GetZoneByIdAsync(int id)
    {
        var zone = await _db.StorageZones
            .Include(z => z.Facility)
            .FirstOrDefaultAsync(z => z.ZoneId == id);

        return zone == null ? null : ToZoneDto(zone);
    }

    public async Task<bool> CreateZoneAsync(CreateStorageZoneRequest request)
    {
        var facilityExists = await _db.Facilities.AnyAsync(f => f.FacilityId == request.FacilityId);
        if (!facilityExists)
            throw new InvalidOperationException(
                $"Facility with ID {request.FacilityId} does not exist.");

        var normalizedName = request.Name.ToLower();
        if (await _db.StorageZones.AnyAsync(z =>
                z.FacilityId     == request.FacilityId &&
                z.Name           != null &&
                z.Name.ToLower() == normalizedName))
            throw new InvalidOperationException(
                $"A zone named '{request.Name}' already exists in this facility.");

        var zone = new StorageZone
        {
            FacilityId         = request.FacilityId,
            Name               = request.Name,
            TemperatureProfile = request.TemperatureProfile,
            Capacity           = request.Capacity
        };

        _db.StorageZones.Add(zone);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateZoneAsync(int id, UpdateStorageZoneRequest request)
    {
        var zone = await _db.StorageZones.FindAsync(id);
        if (zone == null) return false;

        var normalizedName = request.Name.ToLower();
        if (await _db.StorageZones.AnyAsync(z =>
                z.ZoneId         != id &&
                z.FacilityId     == zone.FacilityId &&
                z.Name           != null &&
                z.Name.ToLower() == normalizedName))
            throw new InvalidOperationException(
                $"A zone named '{request.Name}' already exists in this facility.");

        zone.Name               = request.Name;
        zone.TemperatureProfile = request.TemperatureProfile;
        zone.Capacity           = request.Capacity;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteZoneAsync(int id)
    {
        var zone = await _db.StorageZones.FindAsync(id);
        if (zone == null) return false;

        _db.StorageZones.Remove(zone);
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private static FacilityDto ToFacilityDto(Facility f) => new()
    {
        FacilityId = f.FacilityId,
        Name       = f.Name,
        Type       = f.Type,
        Region     = f.Region
    };

    private static StorageZoneDto ToZoneDto(StorageZone z) => new()
    {
        ZoneId             = z.ZoneId,
        FacilityId         = z.FacilityId,
        FacilityName       = z.Facility?.Name ?? string.Empty,
        Name               = z.Name,
        TemperatureProfile = z.TemperatureProfile,
        Capacity           = z.Capacity
    };

}
