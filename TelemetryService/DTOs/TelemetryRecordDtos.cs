using System.ComponentModel.DataAnnotations;

namespace TelemetryService.DTOs;

// ── TelemetryRecord DTOs ──────────────────────────────────────────────────

public class TelemetryRecordDto
{
    public int TelemetryId { get; set; }
    public int SensorId { get; set; }
    // Resolved via EF Include(t => t.SensorDevice) — DeviceType surfaced for convenience.
    public string DeviceType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? Humidity { get; set; }
    public string? Location { get; set; }
    public bool IsExcursion { get; set; }
}

public class CreateTelemetryRecordRequest
{
    [Required(ErrorMessage = "SensorId is required.")]
    public int SensorId { get; set; }

    [Required(ErrorMessage = "Timestamp is required.")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public decimal? Temperature { get; set; }

    public decimal? Humidity { get; set; }

    [MaxLength(200, ErrorMessage = "Location cannot exceed 200 characters.")]
    public string? Location { get; set; }
}

public class UpdateTelemetryRecordRequest
{
    // SensorId is intentionally excluded — reassigning a reading to a different sensor is not allowed.

    [Required(ErrorMessage = "Timestamp is required.")]
    public DateTime Timestamp { get; set; }

    public decimal? Temperature { get; set; }

    public decimal? Humidity { get; set; }

    [MaxLength(200, ErrorMessage = "Location cannot exceed 200 characters.")]
    public string? Location { get; set; }
}
