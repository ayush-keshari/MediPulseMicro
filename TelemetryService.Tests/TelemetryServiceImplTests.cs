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
    public async Task CreateSensorAsync_ReturnsTrue_WhenValid()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        var request = new CreateSensorDeviceRequest
        {
            DeviceName = "Sensor001",
            DeviceType = "Temp",
            AssignedTo = "Shipment",
            AssignedEntityId = 1,
            Status = "Active"
        };

        // Act
        var result = await service.CreateSensorAsync(request);

        // Assert
        Assert.True(result);
        var sensor = await context.SensorDevices.FirstOrDefaultAsync();
        Assert.NotNull(sensor);
        Assert.Equal("Sensor001", sensor!.DeviceName);
        Assert.Equal("Temp", sensor!.DeviceType);
        Assert.Equal("Shipment", sensor!.AssignedTo);
        Assert.Equal(1, sensor!.AssignedEntityId);
        Assert.Equal("Active", sensor!.Status);
    }

    [Fact]
    public async Task GetSensorByIdAsync_ReturnsSensor_WhenExists()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        var sensor = new SensorDevice
        {
            DeviceName = "Sensor002",
            DeviceType = "Humidity",
            AssignedTo = "Zone",
            AssignedEntityId = 5,
            Status = "Active"
        };
        context.SensorDevices.Add(sensor);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetSensorByIdAsync(sensor.SensorId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sensor.SensorId, result.SensorId);
        Assert.Equal("Sensor002", result.DeviceName);
        Assert.Equal("Humidity", result.DeviceType);
        Assert.Equal("Zone", result.AssignedTo);
        Assert.Equal(5, result.AssignedEntityId);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task GetSensorByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Act
        var result = await service.GetSensorByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateTelemetryAsync_ReturnsTrue_WhenValid()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create a sensor first
        var sensorRequest = new CreateSensorDeviceRequest
        {
            DeviceName = "Sensor003",
            DeviceType = "Temp",
            AssignedTo = "Zone",
            AssignedEntityId = 10,
            Status = "Active"
        };
        await service.CreateSensorAsync(sensorRequest);
        var sensor = await context.SensorDevices.FirstOrDefaultAsync();
        Assert.NotNull(sensor);

        var telemetryRequest = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow,
            Temperature = 5.0m,
            Humidity = 50.0m,
            Location = "10.0,20.0"
        };

        // Act
        var result = await service.CreateTelemetryAsync(telemetryRequest);

        // Assert
        Assert.True(result);
        var record = await context.TelemetryRecords.FirstOrDefaultAsync();
        Assert.NotNull(record);
        Assert.Equal(sensor!.SensorId, record.SensorId);
        Assert.Equal(5.0m, record.Temperature);
        Assert.Equal(50.0m, record.Humidity);
        Assert.Equal("10.0,20.0", record.Location);
        Assert.False(record.IsExcursion);
    }

    [Fact]
    public async Task CreateTelemetryAsync_ThrowsWhenSensorDoesNotExist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        var telemetryRequest = new CreateTelemetryRecordRequest
        {
            SensorId = 999,
            Timestamp = DateTime.UtcNow,
            Temperature = 5.0m
        };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateTelemetryAsync(telemetryRequest));
    }

    [Fact]
    public async Task GetTelemetryBySensorAsync_ReturnsRecords_WhenExist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create a sensor
        var sensorRequest = new CreateSensorDeviceRequest
        {
            DeviceName = "Sensor004",
            DeviceType = "Temp",
            AssignedTo = "Zone",
            AssignedEntityId = 20,
            Status = "Active"
        };
        await service.CreateSensorAsync(sensorRequest);
        var sensor = await context.SensorDevices.FirstOrDefaultAsync();
        Assert.NotNull(sensor);

        // Add two telemetry records
        var telemetry1 = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow.AddHours(-2),
            Temperature = 2.0m,
            Humidity = 40.0m
        };
        var telemetry2 = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow.AddHours(-1),
            Temperature = 3.0m,
            Humidity = 45.0m
        };
        await service.CreateTelemetryAsync(telemetry1);
        await service.CreateTelemetryAsync(telemetry2);

        // Act
        var result = await service.GetTelemetryBySensorAsync(sensor!.SensorId);

        // Assert
        Assert.NotNull(result);
        var list = result.ToList();
        Assert.Equal(2, list.Count);
        Assert.All(list, r => Assert.Equal(sensor!.SensorId, r.SensorId));
        Assert.Equal(3.0m, list[0].Temperature);
        Assert.Equal(2.0m, list[1].Temperature);
    }

    [Fact]
    public async Task GetTelemetryBySensorAsync_ReturnsEmpty_WhenNone()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create a sensor with no telemetry
        var sensorRequest = new CreateSensorDeviceRequest
        {
            DeviceName = "Sensor005",
            DeviceType = "Temp",
            AssignedTo = "Zone",
            AssignedEntityId = 30,
            Status = "Active"
        };
        await service.CreateSensorAsync(sensorRequest);
        var sensor = await context.SensorDevices.FirstOrDefaultAsync();
        Assert.NotNull(sensor);

        // Act
        var result = await service.GetTelemetryBySensorAsync(sensor!.SensorId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllTelemetryAsync_ReturnsAllRecords()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create two sensors
        var sensorARequest = new CreateSensorDeviceRequest
        {
            DeviceName = "SensorA",
            DeviceType = "Temp",
            AssignedTo = "Zone",
            AssignedEntityId = 1,
            Status = "Active"
        };
        await service.CreateSensorAsync(sensorARequest);
        var sensorA = await context.SensorDevices.FirstOrDefaultAsync(s => s.DeviceName == "SensorA");
        Assert.NotNull(sensorA);

        var sensorBRequest = new CreateSensorDeviceRequest
        {
            DeviceName = "SensorB",
            DeviceType = "Humidity",
            AssignedTo = "Zone",
            AssignedEntityId = 2,
            Status = "Active"
        };
        await service.CreateSensorAsync(sensorBRequest);
        var sensorB = await context.SensorDevices.FirstOrDefaultAsync(s => s.DeviceName == "SensorB");
        Assert.NotNull(sensorB);

        // Add telemetry for each
        var telemetryA = new CreateTelemetryRecordRequest
        {
            SensorId = sensorA!.SensorId,
            Timestamp = DateTime.UtcNow,
            Temperature = 4.0m
        };
        var telemetryB = new CreateTelemetryRecordRequest
        {
            SensorId = sensorB!.SensorId,
            Timestamp = DateTime.UtcNow,
            Humidity = 60.0m
        };
        await service.CreateTelemetryAsync(telemetryA);
        await service.CreateTelemetryAsync(telemetryB);

        // Act
        var result = await service.GetAllTelemetryAsync();

        // Assert
        Assert.NotNull(result);
        var list = result.ToList();
        Assert.Equal(2, list.Count);
        Assert.Contains(list, r => r.SensorId == sensorA!.SensorId && r.Temperature == 4.0m);
        Assert.Contains(list, r => r.SensorId == sensorB!.SensorId && r.Humidity == 60.0m);
    }

    [Fact]
    public async Task GetTelemetryByIdAsync_ReturnsRecord_WhenExists()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create sensor and telemetry
        var sensorRequest = new CreateSensorDeviceRequest
        {
            DeviceName = "Sensor006",
            DeviceType = "Temp",
            AssignedTo = "Zone",
            AssignedEntityId = 50,
            Status = "Active"
        };
        await service.CreateSensorAsync(sensorRequest);
        var sensor = await context.SensorDevices.FirstOrDefaultAsync();
        Assert.NotNull(sensor);

        var telemetryRequest = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow,
            Temperature = 6.0m
        };
        await service.CreateTelemetryAsync(telemetryRequest);
        var record = await context.TelemetryRecords.FirstOrDefaultAsync();
        Assert.NotNull(record);

        // Act
        var result = await service.GetTelemetryByIdAsync(record!.TelemetryId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(record!.TelemetryId, result.TelemetryId);
        Assert.Equal(sensor!.SensorId, result.SensorId);
        Assert.Equal(6.0m, result.Temperature);
    }

}
