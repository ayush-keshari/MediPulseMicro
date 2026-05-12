using System.ComponentModel.DataAnnotations;

namespace FacilityService.DTOs;

public class CreateFacilityRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
    public string Name { get; set; } = string.Empty;

    // Allowed values: Hospital, Clinic, Warehouse
    [Required(ErrorMessage = "Type is required.")]
    [RegularExpression("^(Hospital|Clinic|Warehouse)$",
        ErrorMessage = "Type must be Hospital, Clinic, or Warehouse.")]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "Region is required.")]
    [MaxLength(100, ErrorMessage = "Region cannot exceed 100 characters.")]
    public string Region { get; set; } = string.Empty;
}
