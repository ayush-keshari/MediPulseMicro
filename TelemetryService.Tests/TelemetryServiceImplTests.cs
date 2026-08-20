using Xunit;
using TelemetryService.Models;

namespace TelemetryService.Tests;

public class TelemetryServiceTests
{
    [Fact]
    public void SensorDevice_HasStatus()
    {
        var sensor = new SensorDevice { Status = "Active" };
        Assert.Equal("Active", sensor.Status);
    }

    [Fact]
    public void SensorDevice_HasDeviceType()
    {
        var sensor = new SensorDevice { DeviceType = "Temp" };
        Assert.Equal("Temp", sensor.DeviceType);
    }

    [Fact]
    public void TelemetryRecord_HasTemperature()
    {
        var record = new TelemetryRecord { Temperature = 4.5m };
        Assert.Equal(4.5m, record.Temperature);
    }

    [Fact]
    public void TelemetryRecord_CanBeExcursion()
    {
        var record = new TelemetryRecord { IsExcursion = true };
        Assert.True(record.IsExcursion);
    }
}