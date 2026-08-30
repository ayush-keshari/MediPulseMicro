using Xunit;
using TelemetryService.Models;
using TelemetryService.Services;
using TelemetryService.DTOs;
using TelemetryService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace TelemetryService.Tests;

public partial class TelemetryServiceImplTests
{
    [Fact]
    public async Task GetTelemetryByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Act
        var result = await service.GetTelemetryByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetExcursionsAsync_ReturnsOnlyExcursions()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create a sensor
        var sensorRequest = new CreateSensorDeviceRequest
        {
            DeviceName = "Sensor007",
            DeviceType = "Temp",
            AssignedTo = "Zone",
            AssignedEntityId = 60,
            Status = "Active"
        };
        await service.CreateSensorAsync(sensorRequest);
        var sensor = await context.SensorDevices.FirstOrDefaultAsync();
        Assert.NotNull(sensor);

        // Add one normal record and one excursion record (temp too high)
        var normal = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow,
            Temperature = 5.0m // within 2-8
        };
        var excursion = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow.AddMinutes(1),
            Temperature = 10.0m // above 8 -> excursion
        };
        await service.CreateTelemetryAsync(normal);
        await service.CreateTelemetryAsync(excursion);

        // Act
        var result = await service.GetExcursionsAsync();

        // Assert
        Assert.NotNull(result);
        var list = result.ToList();
        Assert.Single(list);
        Assert.True(list[0].IsExcursion);
        Assert.Equal(10.0m, list[0].Temperature);
    }

    [Fact]
    public async Task UpdateTelemetryAsync_UpdatesRecord()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create sensor and telemetry
        var sensorRequest = new CreateSensorDeviceRequest
        {
            DeviceName = "Sensor008",
            DeviceType = "Temp",
            AssignedTo = "Zone",
            AssignedEntityId = 70,
            Status = "Active"
        };
        await service.CreateSensorAsync(sensorRequest);
        var sensor = await context.SensorDevices.FirstOrDefaultAsync();
        Assert.NotNull(sensor);

        var now = DateTime.UtcNow;
        var telemetryRequest = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = now,
            Temperature = 6.0m,
            Humidity = 50.0m,
            Location = "5.0,5.0"
        };
        await service.CreateTelemetryAsync(telemetryRequest);
        var record = await context.TelemetryRecords.FirstOrDefaultAsync();
        Assert.NotNull(record);

        // Update request
        var updateRequest = new UpdateTelemetryRecordRequest
        {
            Timestamp = now.AddHours(1),
            Temperature = 7.0m,
            Humidity = 60.0m,
            Location = "6.0,6.0"
        };

        // Act
        var result = await service.UpdateTelemetryAsync(record!.TelemetryId, updateRequest);

        // Assert
        Assert.True(result);
        var updated = await context.TelemetryRecords.FindAsync(record!.TelemetryId);
        Assert.NotNull(updated);
        Assert.Equal(now.AddHours(1), updated.Timestamp);
        Assert.Equal(7.0m, updated.Temperature);
        Assert.Equal(60.0m, updated.Humidity);
        Assert.Equal("6.0,6.0", updated.Location);
        Assert.False(updated.IsExcursion);
    }

    [Fact]
    public async Task DeleteTelemetryAsync_RemovesRecord()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create sensor and telemetry
        var sensorRequest = new CreateSensorDeviceRequest
        {
            DeviceName = "Sensor009",
            DeviceType = "Temp",
            AssignedTo = "Zone",
            AssignedEntityId = 80,
            Status = "Active"
        };
        await service.CreateSensorAsync(sensorRequest);
        var sensor = await context.SensorDevices.FirstOrDefaultAsync();
        Assert.NotNull(sensor);

        var telemetryRequest = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow,
            Temperature = 4.0m
        };
        await service.CreateTelemetryAsync(telemetryRequest);
        var record = await context.TelemetryRecords.FirstOrDefaultAsync();
        Assert.NotNull(record);

        // Act
        var result = await service.DeleteTelemetryAsync(record!.TelemetryId);

        // Assert
        Assert.True(result);
        var deleted = await context.TelemetryRecords.FindAsync(record!.TelemetryId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteSensorAsync_RemovesSensorAndItsTelemetry()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create sensor and telemetry
        var sensorRequest = new CreateSensorDeviceRequest
        {
            DeviceName = "Sensor010",
            DeviceType = "Temp",
            AssignedTo = "Zone",
            AssignedEntityId = 90,
            Status = "Active"
        };
        await service.CreateSensorAsync(sensorRequest);
        var sensor = await context.SensorDevices.FirstOrDefaultAsync();
        Assert.NotNull(sensor);

        var telemetryRequest = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow,
            Temperature = 3.0m
        };
        await service.CreateTelemetryAsync(telemetryRequest);
        // Ensure telemetry exists
        var telemetry = await context.TelemetryRecords.FirstOrDefaultAsync(t => t.SensorId == sensor!.SensorId);
        Assert.NotNull(telemetry);

        // Act
        var result = await service.DeleteSensorAsync(sensor!.SensorId);

        // Assert
        Assert.True(result);
        var deletedSensor = await context.SensorDevices.FindAsync(sensor!.SensorId);
        Assert.Null(deletedSensor);
        var telemetryAfter = await context.TelemetryRecords.FindAsync(telemetry!.TelemetryId);
        Assert.Null(telemetryAfter);
    }

    // NEW TESTS FOR MISSING FUNCTIONALITY

    [Fact]
    public async Task GetAllSensorsAsync_ReturnsEmptyList_WhenNoSensorsExist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Act
        var result = await service.GetAllSensorsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllSensorsAsync_ReturnsSensors_WhenTheyExist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create sensors
        var sensor1 = new SensorDevice { DeviceName = "Sensor001", DeviceType = "Temp", AssignedTo = "Zone", AssignedEntityId = 1, Status = "Active" };
        var sensor2 = new SensorDevice { DeviceName = "Sensor002", DeviceType = "Humidity", AssignedTo = "Shipment", AssignedEntityId = 2, Status = "Inactive" };
        context.SensorDevices.AddRange(sensor1, sensor2);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllSensorsAsync();

        // Assert
        Assert.NotNull(result);
        var sensors = result.ToList();
        Assert.Equal(2, sensors.Count);
        Assert.Equal("Sensor002", sensors[0].DeviceName); // Ordered by SensorId descending
        Assert.Equal("Sensor001", sensors[1].DeviceName);
    }

    [Fact]
    public async Task UpdateSensorAsync_UpdatesSensorSuccessfully()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create a sensor
        var sensor = new SensorDevice { DeviceName = "OldSensor", DeviceType = "Temp", AssignedTo = "Zone", AssignedEntityId = 10, Status = "Active" };
        context.SensorDevices.Add(sensor);
        await context.SaveChangesAsync();

        // Update request
        var updateRequest = new UpdateSensorDeviceRequest
        {
            DeviceName = "NewSensor",
            DeviceType = "Humidity",
            AssignedTo = "Shipment",
            AssignedEntityId = 20,
            Status = "Inactive"
        };

        // Act
        var result = await service.UpdateSensorAsync(sensor.SensorId, updateRequest);

        // Assert
        Assert.True(result);
        var updated = await context.SensorDevices.FindAsync(sensor.SensorId);
        Assert.NotNull(updated);
        Assert.Equal("NewSensor", updated.DeviceName);
        Assert.Equal("Humidity", updated.DeviceType);
        Assert.Equal("Shipment", updated.AssignedTo);
        Assert.Equal(20, updated.AssignedEntityId);
        Assert.Equal("Inactive", updated.Status);
    }

    [Fact]
    public async Task UpdateSensorAsync_ReturnsFalse_WhenSensorNotFound()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Update request
        var updateRequest = new UpdateSensorDeviceRequest
        {
            DeviceName = "NonExistent",
            DeviceType = "Temp",
            AssignedTo = "Zone",
            AssignedEntityId = 999,
            Status = "Active"
        };

        // Act
        var result = await service.UpdateSensorAsync(999, updateRequest);

        // Assert
        Assert.False(result);
    }


    // BOUNDARY CONDITION TESTS FOR EXCURSION DETECTION
}
