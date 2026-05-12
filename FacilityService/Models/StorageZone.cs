namespace FacilityService.Models;

public class StorageZone
{
    public int ZoneId { get; set; }
    public int? FacilityId { get; set; }
    public string? Name { get; set; }
    public string? TemperatureProfile { get; set; }

    // Capacity is decimal to match the monolith (decimal(18,2) in SQL)
    public decimal? Capacity { get; set; }

    public Facility? Facility { get; set; }
}
