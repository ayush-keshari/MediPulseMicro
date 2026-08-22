using Xunit;
using TelemetryService.Models;
using TelemetryService.Services;
using TelemetryService.DTOs;
using TelemetryService.Data;
using Microsoft.EntityFrameworkCore;

namespace TelemetryService.Tests;

public class TelemetryServiceImplTests
{
    private TelemetryDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TelemetryDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task IngestTelemetryAsync_StoresReadingSuccessfully_WhenSensorExists()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create a sensor first
        var sensor = new SensorDevice
        {
            DeviceId = "SENSOR-001",
            DeviceType = "Temperature",
            Location = "Warehouse A",
            Status = "Active"
        };
        context.SensorDevices.Add(sensor);
        await context.SaveChangesAsync();

        var telemetryRecord = new TelemetryRecord
        {
            DeviceId = "SENSOR-001",
            Temperature = 4.5m,
            Humidity = 60.0m,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await service.IngestTelemetryAsync(telemetryRecord);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Record);

        // Verify the record was stored
        var storedRecord = await context.TelemetryRecords
            .FirstOrDefaultAsync(tr => tr.DeviceId == "SENSOR-001" && tr.Temperature == 4.5m);
        Assert.NotNull(storedRecord);
        Assert.Equal(60.0m, storedRecord.Humidity);
    }

    [Fact]
    public async Task IngestTelemetryAsync_ReturnsError_WhenSensorDoesNotExist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        var telemetryRecord = new TelemetryRecord
        {
            DeviceId = "NON-EXISTENT",
            Temperature = 25.0m,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await service.IngestTelemetryAsync(telemetryRecord);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Sensor not found", result.ErrorMessage);
        Assert.Null(result.Record);
    }

    [Fact]
    public async Task GetTelemetryByDeviceIdAsync_ReturnsReadings_WhenSensorHasData()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create a sensor
        var sensor = new SensorDevice
        {
            DeviceId = "SENSOR-002",
            DeviceType = "Temperature",
            Location = "Warehouse B",
            Status = "Active"
        };
        context.SensorDevices.Add(sensor);

        // Add some telemetry records
        var records = new List<TelemetryRecord>
        {
            new TelemetryRecord
            {
                DeviceId = "SENSOR-002",
                Temperature = 20.0m,
                Timestamp = DateTime.UtcNow.AddHours(-2)
            },
            new TelemetryRecord
            {
                DeviceId = "SENSOR-002",
                Temperature = 22.5m,
                Timestamp = DateTime.UtcNow.AddHours(-1)
            },
            new TelemetryRecord
            {
                DeviceId = "SENSOR-002",
                Temperature = 21.0m,
                Timestamp = DateTime.UtcNow
            }
        };

        context.TelemetryRecords.AddRange(records);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetTelemetryByDeviceIdAsync("SENSOR-002", 10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Records);
        Assert.Equal(3, result.Records.Count);
        Assert.All(result.Records, r => Assert.Equal("SENSOR-002", r.DeviceId));
    }

    [Fact]
    public async Task GetTelemetryByDeviceIdAsync_ReturnsEmptyList_WhenSensorHasNoData()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create a sensor with no telemetry data
        var sensor = new SensorDevice
        {
            DeviceId = "SENSOR-003",
            DeviceType = "Temperature",
            Location = "Warehouse C",
            Status = "Active"
        };
        context.SensorDevices.Add(sensor);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetTelemetryByDeviceIdAsync("SENSOR-003", 10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Records);
        Assert.Empty(result.Records);
    }

    [Fact]
    public async Task MarkExcursionAsync_SetsExcursionFlag_WhenTemperatureOutsideBounds()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create a sensor
        var sensor = new SensorDevice
        {
            DeviceId = "SENSOR-004",
            DeviceType = "Temperature",
            Location = "Cold Storage",
            Status = "Active"
        };
        context.SensorDevices.Add(sensor);

        // Add a telemetry record
        var record = new TelemetryRecord
        {
            DeviceId = "SENSOR-004",
            Temperature = 25.0m, // Warm temperature
            Timestamp = DateTime.UtcNow,
            IsExcursion = false
        };
        context.TelemetryRecords.Add(record);
        await context.SaveChangesAsync();

        // Act - Mark as excursion (e.g., for cold storage where 25° is too warm)
        var excursionRequest = new MarkExcursionRequest
        {
            RecordId = record.Id,
            IsExcursion = true
        };
        var result = await service.MarkExcursionAsync(excursionRequest);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify the record was updated
        var updatedRecord = await context.TelemetryRecords.FindAsync(record.Id);
        Assert.True(updatedRecord.IsExcursion);
    }
}