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
            .Select(f => ToFacilityDto(f))
            .ToListAsync();
    }

    public async Task<FacilityDto?> GetFacilityByIdAsync(int id)
    {
        var facility = await _db.Facilities.FindAsync(id);
        return facility == null ? null : ToFacilityDto(facility);
    }

    public async Task<FacilityDto> CreateFacilityAsync(CreateFacilityRequest request)
    {
        var facility = new Facility
        {
            Name   = request.Name,
            Type   = request.Type,
            Region = request.Region
        };

        _db.Facilities.Add(facility);
        await _db.SaveChangesAsync();
        return ToFacilityDto(facility);
    }

    public async Task<FacilityDto?> UpdateFacilityAsync(int id, UpdateFacilityRequest request)
    {
        var facility = await _db.Facilities.FindAsync(id);
        if (facility == null) return null;

        facility.Name   = request.Name;
        facility.Type   = request.Type;
        facility.Region = request.Region;

        await _db.SaveChangesAsync();
        return ToFacilityDto(facility);
    }

    public async Task<bool> DeleteFacilityAsync(int id)
    {
        // Load the facility with all its zones — same cascade logic as the monolith.
        var facility = await _db.Facilities
            .Include(f => f.StorageZones)
            .FirstOrDefaultAsync(f => f.FacilityId == id);

        if (facility == null) return false;

        // Delete all zones that belong to this facility first, then delete the facility.
        // (In the monolith this also deletes ConsumptionRecords, InventoryPositions etc.
        //  but those belong to other microservices — handled there when needed.)
        _db.StorageZones.RemoveRange(facility.StorageZones);
        _db.Facilities.Remove(facility);

        await _db.SaveChangesAsync();
        return true;
    }

    // ── StorageZones ────────────────────────────────────────────────────────

    public async Task<IEnumerable<StorageZoneDto>> GetAllZonesAsync()
    {
        return await _db.StorageZones
            .Include(z => z.Facility)
            .Select(z => ToZoneDto(z))
            .ToListAsync();
    }

    public async Task<IEnumerable<StorageZoneDto>> GetZonesByFacilityAsync(int facilityId)
    {
        return await _db.StorageZones
            .Where(z => z.FacilityId == facilityId)
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

    public async Task<StorageZoneDto> CreateZoneAsync(CreateStorageZoneRequest request)
    {
        var zone = new StorageZone
        {
            FacilityId         = request.FacilityId,
            Name               = request.Name,
            TemperatureProfile = request.TemperatureProfile,
            Capacity           = request.Capacity
        };

        _db.StorageZones.Add(zone);
        await _db.SaveChangesAsync();

        await _db.Entry(zone).Reference(z => z.Facility).LoadAsync();
        return ToZoneDto(zone);
    }

    public async Task<StorageZoneDto?> UpdateZoneAsync(int id, UpdateStorageZoneRequest request)
    {
        var zone = await _db.StorageZones
            .Include(z => z.Facility)
            .FirstOrDefaultAsync(z => z.ZoneId == id);

        if (zone == null) return null;

        zone.Name               = request.Name;
        zone.TemperatureProfile = request.TemperatureProfile;
        zone.Capacity           = request.Capacity;

        await _db.SaveChangesAsync();
        return ToZoneDto(zone);
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
