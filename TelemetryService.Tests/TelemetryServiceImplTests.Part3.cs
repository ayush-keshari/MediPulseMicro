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
    public async Task DetectExcursion_BoundaryTemperatureValues_CorrectlyIdentifiesExcursions()
    {
        // Test boundary values for temperature (2-8°C is normal, outside is excursion)
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create a sensor first
        var sensorRequest = new CreateSensorDeviceRequest
        {
            DeviceName = "BoundaryTestSensor",
            DeviceType = "Temp",
            AssignedTo = "Zone",
            AssignedEntityId = 1,
            Status = "Active"
        };
        await service.CreateSensorAsync(sensorRequest);
        var sensor = await context.SensorDevices.FirstOrDefaultAsync();
        Assert.NotNull(sensor);

        // Test exactly at boundaries (should NOT be excursion)
        var tempExactlyMin = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow,
            Temperature = 2.0m, // Exactly at minimum
            Humidity = 50.0m
        };
        var tempExactlyMax = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow.AddMinutes(1),
            Temperature = 8.0m, // Exactly at maximum
            Humidity = 50.0m
        };

        await service.CreateTelemetryAsync(tempExactlyMin);
        await service.CreateTelemetryAsync(tempExactlyMax);

        var records = await service.GetTelemetryBySensorAsync(sensor!.SensorId);
        var list = records.ToList();
        Assert.False(list[0].IsExcursion); // 2.0°C should NOT be excursion
        Assert.False(list[1].IsExcursion); // 8.0°C should NOT be excursion

        // Test just outside boundaries (should BE excursion)
        var tempBelowMin = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow.AddMinutes(2),
            Temperature = 1.9m, // Just below minimum
            Humidity = 50.0m
        };
        var tempAboveMax = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow.AddMinutes(3),
            Temperature = 8.1m, // Just above maximum
            Humidity = 50.0m
        };

        await service.CreateTelemetryAsync(tempBelowMin);
        await service.CreateTelemetryAsync(tempAboveMax);

        records = await service.GetTelemetryBySensorAsync(sensor!.SensorId);
        list = records.ToList();
        Assert.True(list[0].IsExcursion); // 8.1°C should BE excursion
        Assert.True(list[1].IsExcursion); // 1.9°C should BE excursion
        Assert.False(list[2].IsExcursion); // 8.0°C should NOT be excursion
        Assert.False(list[3].IsExcursion); // 2.0°C should NOT be excursion
    }

    [Fact]
    public async Task DetectExcursion_BoundaryHumidityValues_CorrectlyIdentifiesExcursions()
    {
        // Test boundary values for humidity (30-85% RH is normal, outside is excursion)
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create a sensor first
        var sensorRequest = new CreateSensorDeviceRequest
        {
            DeviceName = "HumidityBoundaryTestSensor",
            DeviceType = "Humidity",
            AssignedTo = "Zone",
            AssignedEntityId = 1,
            Status = "Active"
        };
        await service.CreateSensorAsync(sensorRequest);
        var sensor = await context.SensorDevices.FirstOrDefaultAsync();
        Assert.NotNull(sensor);

        // Test exactly at boundaries (should NOT be excursion)
        var humidityExactlyMin = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow,
            Temperature = 5.0m,
            Humidity = 30.0m // Exactly at minimum
        };
        var humidityExactlyMax = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow.AddMinutes(1),
            Temperature = 5.0m,
            Humidity = 85.0m // Exactly at maximum
        };

        await service.CreateTelemetryAsync(humidityExactlyMin);
        await service.CreateTelemetryAsync(humidityExactlyMax);

        var records = await service.GetTelemetryBySensorAsync(sensor!.SensorId);
        var list = records.ToList();
        Assert.False(list[0].IsExcursion); // 30.0% RH should NOT be excursion
        Assert.False(list[1].IsExcursion); // 85.0% RH should NOT be excursion

        // Test just outside boundaries (should BE excursion)
        var humidityBelowMin = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow.AddMinutes(2),
            Temperature = 5.0m,
            Humidity = 29.9m // Just below minimum
        };
        var humidityAboveMax = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow.AddMinutes(3),
            Temperature = 5.0m,
            Humidity = 85.1m // Just above maximum
        };

        await service.CreateTelemetryAsync(humidityBelowMin);
        await service.CreateTelemetryAsync(humidityAboveMax);

        records = await service.GetTelemetryBySensorAsync(sensor!.SensorId);
        list = records.ToList();
        Assert.True(list[0].IsExcursion); // 85.1% RH should BE excursion
        Assert.True(list[1].IsExcursion); // 29.9% RH should BE excursion
        Assert.False(list[2].IsExcursion); // 85.0% RH should NOT be excursion
        Assert.False(list[3].IsExcursion); // 30.0% RH should NOT be excursion
    }

    // ADDITIONAL TESTS FOR MISSING CASES

    [Fact]
    public async Task UpdateTelemetryAsync_ReturnsFalse_WhenTelemetryNotFound()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        var updateRequest = new UpdateTelemetryRecordRequest
        {
            Timestamp = DateTime.UtcNow,
            Temperature = 5.0m,
            Humidity = 50.0m,
            Location = "0.0,0.0"
        };

        // Act
        var result = await service.UpdateTelemetryAsync(999, updateRequest);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteTelemetryAsync_ReturnsFalse_WhenTelemetryNotFound()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Act
        var result = await service.DeleteTelemetryAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetExcursionsAsync_ReturnsEmptyList_WhenNoExcursionsExist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new TelemetryServiceImpl(context);

        // Create a sensor
        var sensorRequest = new CreateSensorDeviceRequest
        {
            DeviceName = "Sensor011",
            DeviceType = "Temp",
            AssignedTo = "Zone",
            AssignedEntityId = 100,
            Status = "Active"
        };
        await service.CreateSensorAsync(sensorRequest);
        var sensor = await context.SensorDevices.FirstOrDefaultAsync();
        Assert.NotNull(sensor);

        // Add telemetry records that are NOT excursions (within normal range)
        var normalTelemetry1 = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow,
            Temperature = 5.0m, // Within 2-8°C
            Humidity = 50.0m    // Within 30-85% RH
        };
        var normalTelemetry2 = new CreateTelemetryRecordRequest
        {
            SensorId = sensor!.SensorId,
            Timestamp = DateTime.UtcNow.AddHours(1),
            Temperature = 7.0m, // Within 2-8°C
            Humidity = 60.0m    // Within 30-85% RH
        };

        await service.CreateTelemetryAsync(normalTelemetry1);
        await service.CreateTelemetryAsync(normalTelemetry2);

        // Act
        var result = await service.GetExcursionsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
