namespace FacilityService.DTOs;

public class StorageZoneDto
{
    public int ZoneId { get; set; }
    public int? FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? TemperatureProfile { get; set; }
    public decimal? Capacity { get; set; }
}
