using Xunit;
using FacilityService.Models;
using FacilityService.Services;
using FacilityService.DTOs;
using FacilityService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace FacilityService.Tests;

public class FacilityServiceImplTests
{
    private FacilityDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FacilityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new FacilityDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    // ── Facilities Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateFacilityAsync_Succeeds_WithValidData()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        var request = new CreateFacilityRequest
        {
            Name = "City General Hospital",
            Type = "Hospital",
            Region = "North"
        };

        // Act
        var result = await service.CreateFacilityAsync(request);

        // Assert
        Assert.True(result);

        // Verify facility was created
        var facility = await context.Facilities.FirstOrDefaultAsync(
            f => f.Name == "City General Hospital");
        Assert.NotNull(facility);
        Assert.Equal("City General Hospital", facility.Name);
        Assert.Equal("Hospital", facility.Type);
        Assert.Equal("North", facility.Region);
    }

    [Fact]
    public async Task CreateFacilityAsync_ThrowsException_ForDuplicateFacility()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        var request = new CreateFacilityRequest
        {
            Name = "City General Hospital",
            Type = "Hospital",
            Region = "North"
        };

        // Create first facility
        await service.CreateFacilityAsync(request);

        // Try to create duplicate
        var duplicateRequest = new CreateFacilityRequest
        {
            Name = "City General Hospital",
            Type = "Hospital",
            Region = "North"
        };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateFacilityAsync(duplicateRequest));
    }


    [Fact]
    public async Task GetFacilityByIdAsync_ReturnsFacility_WhenExists()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        var facility = new Facility
        {
            Name = "City General Hospital",
            Type = "Hospital",
            Region = "North"
        };
        context.Facilities.Add(facility);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetFacilityByIdAsync(facility.FacilityId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(facility.FacilityId, result.FacilityId);
        Assert.Equal("City General Hospital", result.Name);
        Assert.Equal("Hospital", result.Type);
        Assert.Equal("North", result.Region);
    }

    [Fact]
    public async Task GetFacilityByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        // Act
        var result = await service.GetFacilityByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllFacilitiesAsync_ReturnsEmptyList_WhenNoFacilitiesExist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        // Act
        var result = await service.GetAllFacilitiesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllFacilitiesAsync_ReturnsFacilities_WhenTheyExist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        // Create facilities
        var facility1 = new Facility { Name = "Hospital A", Type = "Hospital", Region = "North" };
        var facility2 = new Facility { Name = "Clinic B", Type = "Clinic", Region = "South" };
        context.Facilities.AddRange(facility1, facility2);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllFacilitiesAsync();

        // Assert
        Assert.NotNull(result);
        var facilities = result.ToList();
        Assert.Equal(2, facilities.Count);
        // Should be ordered by FacilityId descending
        Assert.Equal("Clinic B", facilities[0].Name); // Higher ID first
        Assert.Equal("Hospital A", facilities[1].Name);
    }

    [Fact]
    public async Task UpdateFacilityAsync_Succeeds_WhenFacilityExists()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        var facility = new Facility
        {
            Name = "Old Hospital",
            Type = "Hospital",
            Region = "North"
        };
        context.Facilities.Add(facility);
        await context.SaveChangesAsync();

        var request = new UpdateFacilityRequest
        {
            Name = "New Hospital",
            Type = "Hospital",
            Region = "South"
        };

        // Act
        var result = await service.UpdateFacilityAsync(facility.FacilityId, request);

        // Assert
        Assert.True(result);

        // Verify facility was updated
        var updatedFacility = await context.Facilities.FindAsync(facility.FacilityId);
        Assert.NotNull(updatedFacility);
        Assert.Equal("New Hospital", updatedFacility.Name);
        Assert.Equal("Hospital", updatedFacility.Type);
        Assert.Equal("South", updatedFacility.Region);
    }

    [Fact]
    public async Task UpdateFacilityAsync_ReturnsFalse_WhenFacilityNotFound()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        var request = new UpdateFacilityRequest
        {
            Name = "New Hospital",
            Type = "Hospital",
            Region = "South"
        };

        // Act
        var result = await service.UpdateFacilityAsync(999, request);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateFacilityAsync_ThrowsException_ForDuplicateFacility()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        // Create two facilities
        var facility1 = new Facility { Name = "Hospital A", Type = "Hospital", Region = "North" };
        var facility2 = new Facility { Name = "Hospital B", Type = "Hospital", Region = "South" };
        context.Facilities.AddRange(facility1, facility2);
        await context.SaveChangesAsync();

        // Try to update facility2 to have same name/type/region as facility1
        var updateRequest = new UpdateFacilityRequest
        {
            Name = "Hospital A", // Same as facility1
            Type = "Hospital",   // Same as facility1
            Region = "North"     // Same as facility1
        };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.UpdateFacilityAsync(facility2.FacilityId, updateRequest));
    }

    [Fact]
    public async Task DeleteFacilityAsync_Succeeds_WhenFacilityExists()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        var facility = new Facility
        {
            Name = "City General Hospital",
            Type = "Hospital",
            Region = "North"
        };
        context.Facilities.Add(facility);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteFacilityAsync(facility.FacilityId);

        // Assert
        Assert.True(result);

        // Verify facility was deleted
        var deletedFacility = await context.Facilities.FindAsync(facility.FacilityId);
        Assert.Null(deletedFacility);
    }

    [Fact]
    public async Task DeleteFacilityAsync_ReturnsFalse_WhenFacilityNotFound()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        // Act
        var result = await service.DeleteFacilityAsync(999);

        // Assert
        Assert.False(result);
    }

    // ── StorageZones Tests ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateZoneAsync_Succeeds_WithValidData()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        // Create a facility first
        var facilityRequest = new CreateFacilityRequest
        {
            Name = "City General Hospital",
            Type = "Hospital",
            Region = "North"
        };
        await service.CreateFacilityAsync(facilityRequest);
        var facility = await context.Facilities.FirstOrDefaultAsync();
        Assert.NotNull(facility);

        var zoneRequest = new CreateStorageZoneRequest
        {
            FacilityId = facility.FacilityId,
            Name = "Zone A",
            TemperatureProfile = "Refrigerated",
            Capacity = 1000.50m
        };

        // Act
        var result = await service.CreateZoneAsync(zoneRequest);

        // Assert
        Assert.True(result);

        // Verify zone was created
        var zone = await context.StorageZones.FirstOrDefaultAsync(
            z => z.Name == "Zone A" && z.FacilityId == facility.FacilityId);
        Assert.NotNull(zone);
        Assert.Equal(facility.FacilityId, zone.FacilityId);
        Assert.Equal("Zone A", zone.Name);
        Assert.Equal("Refrigerated", zone.TemperatureProfile);
        Assert.Equal(1000.50m, zone.Capacity);
    }

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

    [Fact]
    public async Task DeleteZoneAsync_Succeeds_WhenZoneExists()
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
        var result = await service.DeleteZoneAsync(zone.ZoneId);

        // Assert
        Assert.True(result);

        // Verify zone was deleted
        var deletedZone = await context.StorageZones.FindAsync(zone.ZoneId);
        Assert.Null(deletedZone);
    }

    [Fact]
    public async Task DeleteZoneAsync_ReturnsFalse_WhenZoneNotFound()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new FacilityServiceImpl(context);

        // Act
        var result = await service.DeleteZoneAsync(999);

        // Assert
        Assert.False(result);
    }

    // ── CASCADE DELETE TESTS ────────────────────────────────────────────────

    [Fact]
    public async Task DeleteFacilityAsync_DeletesAssociatedZones()
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

        // Create zones associated with the facility
        var zone1 = new StorageZone { FacilityId = facility.FacilityId, Name = "Zone A", TemperatureProfile = "Refrigerated", Capacity = 1000.50m };
        var zone2 = new StorageZone { FacilityId = facility.FacilityId, Name = "Zone B", TemperatureProfile = "Freezer", Capacity = 500.25m };
        context.StorageZones.AddRange(zone1, zone2);
        await context.SaveChangesAsync();

        // Verify zones exist
        var zonesBefore = await context.StorageZones.Where(z => z.FacilityId == facility.FacilityId).ToListAsync();
        Assert.Equal(2, zonesBefore.Count);

        // Act
        var result = await service.DeleteFacilityAsync(facility.FacilityId);

        // Assert
        Assert.True(result);

        // Verify facility was deleted
        var deletedFacility = await context.Facilities.FindAsync(facility.FacilityId);
        Assert.Null(deletedFacility);

        // Verify associated zones were also deleted
        var zonesAfter = await context.StorageZones.Where(z => z.FacilityId == facility.FacilityId).ToListAsync();
        Assert.Empty(zonesAfter);
    }
}
