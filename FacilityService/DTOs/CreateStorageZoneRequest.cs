using System.ComponentModel.DataAnnotations;

namespace FacilityService.DTOs;

public class CreateStorageZoneRequest
{
    [Required(ErrorMessage = "FacilityId is required.")]
    public int FacilityId { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "TemperatureProfile is required.")]
    [RegularExpression("^(Ambient|Refrigerated|Freezer)$",
        ErrorMessage = "TemperatureProfile must be Ambient, Refrigerated, or Freezer.")]
    public string TemperatureProfile { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Capacity must be greater than 0.")]
    public decimal Capacity { get; set; }
}
