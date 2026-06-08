using TelemetryService.DTOs;

namespace TelemetryService.Services;

// TelemetryService owns: SensorDevice, TelemetryRecord.
// All EF navigation is local — no cross-service HTTP calls needed.
public interface ITelemetryService
{
    // ── SensorDevices ─────────────────────────────────────────────────────
    Task<IEnumerable<SensorDeviceDto>> GetAllSensorsAsync();
    Task<SensorDeviceDto?> GetSensorByIdAsync(int id);
    Task<bool> CreateSensorAsync(CreateSensorDeviceRequest request);
    Task<bool> UpdateSensorAsync(int id, UpdateSensorDeviceRequest request);

    // Delete blocked if sensor has associated TelemetryRecords (throws InvalidOperationException).
    Task<bool> DeleteSensorAsync(int id);

    // Returns all TelemetryRecords belonging to the given sensor.
    Task<IEnumerable<TelemetryRecordDto>> GetTelemetryBySensorAsync(int sensorId);

    // ── TelemetryRecords ──────────────────────────────────────────────────
    Task<IEnumerable<TelemetryRecordDto>> GetAllTelemetryAsync();
    Task<TelemetryRecordDto?> GetTelemetryByIdAsync(int id);

    // Returns only records where IsExcursion = true.
    Task<IEnumerable<TelemetryRecordDto>> GetExcursionsAsync();

    // Excursion detection runs automatically on ingest.
    Task<bool> CreateTelemetryAsync(CreateTelemetryRecordRequest request);
    Task<bool> UpdateTelemetryAsync(int id, UpdateTelemetryRecordRequest request);
    Task<bool> DeleteTelemetryAsync(int id);
}
