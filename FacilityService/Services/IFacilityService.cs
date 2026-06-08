using FacilityService.DTOs;

namespace FacilityService.Services;

public interface IFacilityService
{
    // ── Facilities ────────────────────────────────────────────────────────
    Task<IEnumerable<FacilityDto>> GetAllFacilitiesAsync();
    Task<FacilityDto?> GetFacilityByIdAsync(int id);
    Task<bool> CreateFacilityAsync(CreateFacilityRequest request);
    Task<bool> UpdateFacilityAsync(int id, UpdateFacilityRequest request);
    Task<bool> DeleteFacilityAsync(int id);

    // ── StorageZones ──────────────────────────────────────────────────────
    Task<IEnumerable<StorageZoneDto>> GetAllZonesAsync();
    Task<IEnumerable<StorageZoneDto>> GetZonesByFacilityAsync(int facilityId);
    Task<StorageZoneDto?> GetZoneByIdAsync(int id);
    Task<bool> CreateZoneAsync(CreateStorageZoneRequest request);
    Task<bool> UpdateZoneAsync(int id, UpdateStorageZoneRequest request);
    Task<bool> DeleteZoneAsync(int id);


}
