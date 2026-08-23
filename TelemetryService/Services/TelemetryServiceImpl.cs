using Microsoft.EntityFrameworkCore;
using TelemetryService.Data;
using TelemetryService.DTOs;
using TelemetryService.Models;

namespace TelemetryService.Services;

public class TelemetryServiceImpl : ITelemetryService
{
    private readonly TelemetryDbContext _db;

    public TelemetryServiceImpl(TelemetryDbContext db) => _db = db;

    // ── SensorDevices ─────────────────────────────────────────────────────

    public async Task<IEnumerable<SensorDeviceDto>> GetAllSensorsAsync()
        => await _db.SensorDevices
            .OrderByDescending(s => s.SensorId)
            .Select(s => ToSensorDto(s))
            .ToListAsync();

    public async Task<SensorDeviceDto?> GetSensorByIdAsync(int id)
    {
        var s = await _db.SensorDevices.FindAsync(id);
        return s == null ? null : ToSensorDto(s);
    }

    public async Task<bool> CreateSensorAsync(CreateSensorDeviceRequest request)
    {
        var sensor = new SensorDevice
        {
            DeviceName = request.DeviceName,
            DeviceType = request.DeviceType,
            AssignedTo = request.AssignedTo,
            AssignedEntityId = request.AssignedEntityId,
            Status = request.Status
        };
        _db.SensorDevices.Add(sensor);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateSensorAsync(int id, UpdateSensorDeviceRequest request)
    {
        var sensor = await _db.SensorDevices.FindAsync(id);
        if (sensor == null) return false;

        sensor.DeviceName = request.DeviceName;
        sensor.DeviceType = request.DeviceType;
        sensor.AssignedTo = request.AssignedTo;
        sensor.AssignedEntityId = request.AssignedEntityId;
        sensor.Status = request.Status;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteSensorAsync(int id)
    {
        var sensor = await _db.SensorDevices
            .Include(s => s.TelemetryRecords)
            .FirstOrDefaultAsync(s => s.SensorId == id);

        if (sensor == null) return false;

        _db.TelemetryRecords.RemoveRange(sensor.TelemetryRecords);
        _db.SensorDevices.Remove(sensor);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<TelemetryRecordDto>> GetTelemetryBySensorAsync(int sensorId)
        => await _db.TelemetryRecords
            .Where(t => t.SensorId == sensorId)
            .OrderByDescending(t => t.TelemetryId)
            .Include(t => t.SensorDevice)
            .Select(t => ToTelemetryDto(t))
            .ToListAsync();

    // ── TelemetryRecords ──────────────────────────────────────────────────

    public async Task<IEnumerable<TelemetryRecordDto>> GetAllTelemetryAsync()
        => await _db.TelemetryRecords
            .OrderByDescending(t => t.TelemetryId)
            .Include(t => t.SensorDevice)
            .Select(t => ToTelemetryDto(t))
            .ToListAsync();

    public async Task<TelemetryRecordDto?> GetTelemetryByIdAsync(int id)
    {
        var t = await _db.TelemetryRecords
            .Include(t => t.SensorDevice)
            .FirstOrDefaultAsync(t => t.TelemetryId == id);
        return t == null ? null : ToTelemetryDto(t);
    }

    public async Task<IEnumerable<TelemetryRecordDto>> GetExcursionsAsync()
        => await _db.TelemetryRecords
            .Where(t => t.IsExcursion)
            .OrderByDescending(t => t.TelemetryId)
            .Include(t => t.SensorDevice)
            .Select(t => ToTelemetryDto(t))
            .ToListAsync();

    public async Task<bool> CreateTelemetryAsync(CreateTelemetryRecordRequest request)
    {
        var sensorExists = await _db.SensorDevices.AnyAsync(s => s.SensorId == request.SensorId);
        if (!sensorExists)
            throw new InvalidOperationException(
                $"Sensor with ID {request.SensorId} does not exist.");

        var record = new TelemetryRecord
        {
            SensorId = request.SensorId,
            Timestamp = request.Timestamp,
            Temperature = request.Temperature,
            Humidity = request.Humidity,
            Location = request.Location
        };

        DetectExcursion(record);

        _db.TelemetryRecords.Add(record);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateTelemetryAsync(int id, UpdateTelemetryRecordRequest request)
    {
        var record = await _db.TelemetryRecords.FindAsync(id);
        if (record == null) return false;

        record.Timestamp = request.Timestamp;
        record.Temperature = request.Temperature;
        record.Humidity = request.Humidity;
        record.Location = request.Location;

        record.IsExcursion = false;
        DetectExcursion(record);

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTelemetryAsync(int id)
    {
        var record = await _db.TelemetryRecords.FindAsync(id);
        if (record == null) return false;

        _db.TelemetryRecords.Remove(record);
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Private helpers ───────────────────────────────────────────────────

    // Safe pharmaceutical cold-chain ranges: 2–8 °C, 30–85 % RH.
    private static void DetectExcursion(TelemetryRecord record)
    {
        bool excursion = false;

        if (record.Temperature.HasValue)
        {
            var t = record.Temperature.Value;
            if (t > 8.0m || t < 2.0m) excursion = true;
        }

        if (record.Humidity.HasValue)
        {
            var h = record.Humidity.Value;
            if (h > 85.0m || h < 30.0m) excursion = true;
        }

        record.IsExcursion = excursion;
    }

    private static SensorDeviceDto ToSensorDto(SensorDevice s) => new()
    {
        SensorId = s.SensorId,
        DeviceName = s.DeviceName,
        DeviceType = s.DeviceType,
        AssignedTo = s.AssignedTo,
        AssignedEntityId = s.AssignedEntityId,
        Status = s.Status
    };

    private static TelemetryRecordDto ToTelemetryDto(TelemetryRecord t) => new()
    {
        TelemetryId = t.TelemetryId,
        SensorId = t.SensorId,
        DeviceType = t.SensorDevice?.DeviceType ?? string.Empty,
        Timestamp = t.Timestamp,
        Temperature = t.Temperature,
        Humidity = t.Humidity,
        Location = t.Location,
        IsExcursion = t.IsExcursion
    };
}
