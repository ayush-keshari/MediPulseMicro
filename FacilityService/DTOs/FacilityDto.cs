namespace FacilityService.DTOs;

public class FacilityDto
{
    public int FacilityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Region { get; set; }
}
