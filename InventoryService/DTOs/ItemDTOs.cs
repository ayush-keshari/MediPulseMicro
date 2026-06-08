using System.ComponentModel.DataAnnotations;

namespace InventoryService.DTOs;

public class CreateItemRequest
{
    [Required, MaxLength(50)]
    public string ItemCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    [RegularExpression(@"^[a-zA-Z/ ]+$", ErrorMessage = "Unit must contain only letters, spaces and /.")]
    public string Unit { get; set; } = string.Empty;

    [MaxLength(50)]
    public string StorageRequirement { get; set; } = "Ambient";

    [Range(0, int.MaxValue)]
    public int SafetyStock { get; set; }
}

public class UpdateItemRequest
{
    [MaxLength(150)]
    public string? Name { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; }

    [MaxLength(20)]
    [RegularExpression(@"^[a-zA-Z/ ]+$", ErrorMessage = "Unit must contain only letters, spaces and /.")]
    public string? Unit { get; set; }

    [MaxLength(50)]
    public string? StorageRequirement { get; set; }

    [Range(0, int.MaxValue)]
    public int? SafetyStock { get; set; }
}

public class ItemResponse
{
    public int    ItemId             { get; set; }
    public string ItemCode           { get; set; } = string.Empty;
    public string Name               { get; set; } = string.Empty;
    public string Category           { get; set; } = string.Empty;
    public string Unit               { get; set; } = string.Empty;
    public string StorageRequirement { get; set; } = string.Empty;
    public int    SafetyStock        { get; set; }
    public int    TotalStock         { get; set; }   // sum of all lot quantities
}
