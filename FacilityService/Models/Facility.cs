namespace FacilityService.Models;

public class Facility
{
    public int FacilityId { get; set; }
    public string Name { get; set; } = null!;
    public string? Type { get; set; }
    public string? Region { get; set; }

    public ICollection<StorageZone> StorageZones { get; set; } = new List<StorageZone>();
}
