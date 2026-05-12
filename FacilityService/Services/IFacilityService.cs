using FacilityService.DTOs;

namespace FacilityService.Services;

public interface IFacilityService
{
    Task<IEnumerable<FacilityDto>> GetAllFacilitiesAsync();
    Task<FacilityDto?> GetFacilityByIdAsync(int id);
    Task<FacilityDto> CreateFacilityAsync(CreateFacilityRequest request);
    Task<FacilityDto?> UpdateFacilityAsync(int id, UpdateFacilityRequest request);
    Task<bool> DeleteFacilityAsync(int id);

    Task<IEnumerable<StorageZoneDto>> GetAllZonesAsync();
    Task<IEnumerable<StorageZoneDto>> GetZonesByFacilityAsync(int facilityId);
    Task<StorageZoneDto?> GetZoneByIdAsync(int id);
    Task<StorageZoneDto> CreateZoneAsync(CreateStorageZoneRequest request);
    Task<StorageZoneDto?> UpdateZoneAsync(int id, UpdateStorageZoneRequest request);
    Task<bool> DeleteZoneAsync(int id);
}
