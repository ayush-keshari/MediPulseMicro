using System.ComponentModel.DataAnnotations;

namespace TelemetryService.Models;

// SensorDevice represents a physical IoT sensor deployed to monitor cold chain conditions.
// A sensor is assigned to either a StorageZone or a Shipment (cross-service reference by ID only).
public class SensorDevice
{
    // Primary key — maps to column SensorID in the database.
    [Key]
    public int SensorId { get; set; }

    // Human-readable label for this sensor (e.g. "Cold Room Alpha #1").
    [Required, MaxLength(100)]
    public string DeviceName { get; set; } = string.Empty;

    // The kind of measurement this sensor captures: Temp | Humidity | GPS.
    [Required, MaxLength(50)]
    public string DeviceType { get; set; } = string.Empty;

    // The entity type this sensor monitors: Shipment | Zone.
    [Required, MaxLength(50)]
    public string AssignedTo { get; set; } = string.Empty;

    // The ID of the Zone or Shipment this sensor is attached to.
    // Nullable because a sensor may be unassigned (e.g., in a warehouse stockpile).
    // No EF navigation — cross-service reference resolved by the caller.
    public int? AssignedEntityId { get; set; }

    // Operational state of the sensor: Active | Inactive | Faulty.
    [Required, MaxLength(50)]
    public string Status { get; set; } = "Active";

    // Navigation — one SensorDevice produces many TelemetryRecords.
    public ICollection<TelemetryRecord> TelemetryRecords { get; set; } = new List<TelemetryRecord>();
}
