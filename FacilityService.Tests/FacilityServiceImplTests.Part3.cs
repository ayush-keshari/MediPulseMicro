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
