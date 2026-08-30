using Xunit;
using FacilityService.Models;
using FacilityService.Services;
using FacilityService.DTOs;
using FacilityService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace FacilityService.Tests;

public partial class FacilityServiceImplTests
{
    [Fact]
    public async Task CreateZoneAsync_ThrowsException_WhenFacilityDoesNotExist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        var zoneRequest = new CreateStorageZoneRequest
        {
            FacilityId = 999, // Non-existent facility
            Name = "Zone A",
            TemperatureProfile = "Refrigerated",
            Capacity = 1000.50m
        };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateZoneAsync(zoneRequest));
    }

    [Fact]
    public async Task CreateZoneAsync_ThrowsException_ForDuplicateZoneName()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        // Create a facility
        var facilityRequest = new CreateFacilityRequest
        {
            Name = "City General Hospital",
            Type = "Hospital",
            Region = "North"
        };
        await service.CreateFacilityAsync(facilityRequest);
        var facility = await context.Facilities.FirstOrDefaultAsync();
        Assert.NotNull(facility);

        // Create first zone
        var zoneRequest1 = new CreateStorageZoneRequest
        {
            FacilityId = facility.FacilityId,
            Name = "Zone A",
            TemperatureProfile = "Refrigerated",
            Capacity = 1000.50m
        };
        await service.CreateZoneAsync(zoneRequest1);

        // Try to create duplicate zone (case-insensitive)
        var zoneRequest2 = new CreateStorageZoneRequest
        {
            FacilityId = facility.FacilityId,
            Name = "zone a", // Same name, different case
            TemperatureProfile = "Freezer",
            Capacity = 500.25m
        };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateZoneAsync(zoneRequest2));
    }



    [Fact]
    public async Task GetZoneByIdAsync_ReturnsZone_WhenExists()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        // Create a facility
        var facilityRequest = new CreateFacilityRequest
        {
            Name = "City General Hospital",
            Type = "Hospital",
            Region = "North"
        };
        await service.CreateFacilityAsync(facilityRequest);
        var facility = await context.Facilities.FirstOrDefaultAsync();
        Assert.NotNull(facility);

        // Create a zone
        var zone = new StorageZone
        {
            FacilityId = facility.FacilityId,
            Name = "Zone A",
            TemperatureProfile = "Refrigerated",
            Capacity = 1000.50m
        };
        context.StorageZones.Add(zone);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetZoneByIdAsync(zone.ZoneId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(zone.ZoneId, result.ZoneId);
        Assert.Equal(facility.FacilityId, result.FacilityId);
        Assert.Equal("Zone A", result.Name);
        Assert.Equal("Refrigerated", result.TemperatureProfile);
        Assert.Equal(1000.50m, result.Capacity);
        Assert.Equal(facility.Name, result.FacilityName);
    }

    [Fact]
    public async Task GetZoneByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        // Act
        var result = await service.GetZoneByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetZonesByFacilityAsync_ReturnsEmptyList_WhenNoZonesExist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        // Create a facility with no zones
        var facilityRequest = new CreateFacilityRequest
        {
            Name = "City General Hospital",
            Type = "Hospital",
            Region = "North"
        };
        await service.CreateFacilityAsync(facilityRequest);
        var facility = await context.Facilities.FirstOrDefaultAsync();
        Assert.NotNull(facility);

        // Act
        var result = await service.GetZonesByFacilityAsync(facility.FacilityId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetZonesByFacilityAsync_ReturnsZones_WhenTheyExist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        // Create a facility
        var facilityRequest = new CreateFacilityRequest
        {
            Name = "City General Hospital",
            Type = "Hospital",
            Region = "North"
        };
        await service.CreateFacilityAsync(facilityRequest);
        var facility = await context.Facilities.FirstOrDefaultAsync();
        Assert.NotNull(facility);

        // Create zones
        var zone1 = new StorageZone { FacilityId = facility.FacilityId, Name = "Zone A", TemperatureProfile = "Refrigerated", Capacity = 1000.50m };
        var zone2 = new StorageZone { FacilityId = facility.FacilityId, Name = "Zone B", TemperatureProfile = "Freezer", Capacity = 500.25m };
        context.StorageZones.AddRange(zone1, zone2);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetZonesByFacilityAsync(facility.FacilityId);

        // Assert
        Assert.NotNull(result);
        var zones = result.ToList();
        Assert.Equal(2, zones.Count);
        // Should be ordered by ZoneId descending
        Assert.Equal("Zone B", zones[0].Name); // Higher ID first
        Assert.Equal("Zone A", zones[1].Name);
    }

    [Fact]
    public async Task GetAllZonesAsync_ReturnsEmptyList_WhenNoZonesExist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        // Act
        var result = await service.GetAllZonesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllZonesAsync_ReturnsZones_WhenTheyExist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        // Create facilities
        var facility1 = new Facility { Name = "Hospital A", Type = "Hospital", Region = "North" };
        var facility2 = new Facility { Name = "Clinic B", Type = "Clinic", Region = "South" };
        context.Facilities.AddRange(facility1, facility2);
        await context.SaveChangesAsync();

        // Create zones
        var zone1 = new StorageZone { FacilityId = facility1.FacilityId, Name = "Zone A", TemperatureProfile = "Refrigerated", Capacity = 1000.50m };
        var zone2 = new StorageZone { FacilityId = facility2.FacilityId, Name = "Zone B", TemperatureProfile = "Freezer", Capacity = 500.25m };
        context.StorageZones.AddRange(zone1, zone2);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllZonesAsync();

        // Assert
        Assert.NotNull(result);
        var zones = result.ToList();
        Assert.Equal(2, zones.Count);
        // Should be ordered by ZoneId descending
        Assert.Equal("Zone B", zones[0].Name); // Higher ID first
        Assert.Equal("Zone A", zones[1].Name);
    }

    [Fact]
    public async Task UpdateZoneAsync_Succeeds_WhenZoneExists()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        // Create a facility
        var facilityRequest = new CreateFacilityRequest
        {
            Name = "City General Hospital",
            Type = "Hospital",
            Region = "North"
        };
        await service.CreateFacilityAsync(facilityRequest);
        var facility = await context.Facilities.FirstOrDefaultAsync();
        Assert.NotNull(facility);

        // Create a zone
        var zone = new StorageZone
        {
            FacilityId = facility.FacilityId,
            Name = "Zone A",
            TemperatureProfile = "Refrigerated",
            Capacity = 1000.50m
        };
        context.StorageZones.Add(zone);
        await context.SaveChangesAsync();

        var request = new UpdateStorageZoneRequest
        {
            Name = "Zone B",
            TemperatureProfile = "Freezer",
            Capacity = 2000.75m
        };

        // Act
        var result = await service.UpdateZoneAsync(zone.ZoneId, request);

        // Assert
        Assert.True(result);

        // Verify zone was updated
        var updatedZone = await context.StorageZones.FindAsync(zone.ZoneId);
        Assert.NotNull(updatedZone);
        Assert.Equal("Zone B", updatedZone.Name);
        Assert.Equal("Freezer", updatedZone.TemperatureProfile);
        Assert.Equal(2000.75m, updatedZone.Capacity);
    }

    [Fact]
    public async Task UpdateZoneAsync_ReturnsFalse_WhenZoneNotFound()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        var request = new UpdateStorageZoneRequest
        {
            Name = "Zone B",
            TemperatureProfile = "Freezer",
            Capacity = 2000.75m
        };

        // Act
        var result = await service.UpdateZoneAsync(999, request);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateZoneAsync_ThrowsException_ForDuplicateZoneName()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        // Create a facility
        var facilityRequest = new CreateFacilityRequest
        {
            Name = "City General Hospital",
            Type = "Hospital",
            Region = "North"
        };
        await service.CreateFacilityAsync(facilityRequest);
        var facility = await context.Facilities.FirstOrDefaultAsync();
        Assert.NotNull(facility);

        // Create two zones
        var zone1 = new StorageZone { FacilityId = facility.FacilityId, Name = "Zone A", TemperatureProfile = "Refrigerated", Capacity = 1000.50m };
        var zone2 = new StorageZone { FacilityId = facility.FacilityId, Name = "Zone B", TemperatureProfile = "Freezer", Capacity = 500.25m };
        context.StorageZones.AddRange(zone1, zone2);
        await context.SaveChangesAsync();

        // Try to update zone2 to have same name as zone1 (case-insensitive)
        var updateRequest = new UpdateStorageZoneRequest
        {
            Name = "zone a", // Same as zone1, different case
            TemperatureProfile = "Ambient",
            Capacity = 750.00m
        };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.UpdateZoneAsync(zone2.ZoneId, updateRequest));
    }

}
