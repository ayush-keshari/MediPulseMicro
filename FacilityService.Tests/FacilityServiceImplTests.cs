using Xunit;
using FacilityService.Models;
using FacilityService.Services;
using FacilityService.DTOs;
using FacilityService.Data;
using Microsoft.EntityFrameworkCore;

namespace FacilityService.Tests
{
    public class FacilityServiceTests
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

        [Fact]
        public async Task CreateFacilityAsync_ThrowsException_WhenDuplicateFacilityExists()
        {
            // Arrange
            await using var context = CreateInMemoryDbContext();
            var service = new FacilityServiceImpl(context);

            // Create first facility
            var firstRequest = new CreateFacilityRequest
            {
                Name = "Apollo Hospital",
                Type = "Hospital",
                Region = "North"
            };

            await service.CreateFacilityAsync(firstRequest);

            // Try to create duplicate (same name, type, region)
            var duplicateRequest = new CreateFacilityRequest
            {
                Name = "Apollo Hospital", // Same name
                Type = "Hospital",        // Same type
                Region = "North"          // Same region
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateFacilityAsync(duplicateRequest));
        }

        [Fact]
        public async Task CreateFacilityAsync_CreatesFacilitySuccessfully_WhenNoDuplicateExists()
        {
            // Arrange
            await using var context = CreateInMemoryDbContext();
            var service = new FacilityServiceImpl(context);

            var request = new CreateFacilityRequest
            {
                Name = "City Clinic",
                Type = "Clinic",
                Region = "South"
            };

            // Act
            var result = await service.CreateFacilityAsync(request);

            // Assert
            Assert.True(result);

            // Verify facility was created
            var facility = await context.Facilities
                .FirstOrDefaultAsync(f => f.Name == "City Clinic" && f.Type == "Clinic" && f.Region == "South");
            Assert.NotNull(facility);
            Assert.Equal("City Clinic", facility.Name);
            Assert.Equal("Clinic", facility.Type);
            Assert.Equal("South", facility.Region);
        }

        [Fact]
        public async Task CreateZoneAsync_ThrowsException_WhenFacilityDoesNotExist()
        {
            // Arrange
            await using var context = CreateInMemoryDbContext();
            var service = new FacilityServiceImpl(context);

            var request = new CreateStorageZoneRequest
            {
                FacilityId = 999, // Non-existent facility
                Name = "Cold Zone A",
                TemperatureProfile = "Freezer",
                Capacity = 1000.5m
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateZoneAsync(request));
        }

        [Fact]
        public async Task CreateZoneAsync_ThrowsException_WhenDuplicateZoneExistsInFacility()
        {
            // Arrange
            await using var context = CreateInMemoryDbContext();
            var service = new FacilityServiceImpl(context);

            // Create a facility first
            var facilityRequest = new CreateFacilityRequest
            {
                Name = "General Hospital",
                Type = "Hospital",
                Region = "East"
            };
            await service.CreateFacilityAsync(facilityRequest);

            // Get the created facility to use its ID
            var facility = await context.Facilities.FirstOrDefaultAsync(f => f.Name == "General Hospital");
            Assert.NotNull(facility);

            // Create first zone
            var firstZoneRequest = new CreateStorageZoneRequest
            {
                FacilityId = facility.FacilityId,
                Name = "Cold Storage A",
                TemperatureProfile = "Refrigerated",
                Capacity = 500.0m
            };
            await service.CreateZoneAsync(firstZoneRequest);

            // Try to create duplicate zone (same name in same facility)
            var duplicateZoneRequest = new CreateStorageZoneRequest
            {
                FacilityId = facility.FacilityId, // Same facility
                Name = "Cold Storage A",          // Same name
                TemperatureProfile = "Freezer",   // Different temp profile
                Capacity = 300.0m                 // Different capacity
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateZoneAsync(duplicateZoneRequest));
        }

        [Fact]
        public async Task CreateZoneAsync_CreatesZoneSuccessfully_WhenValid()
        {
            // Arrange
            await using var context = CreateInMemoryDbContext();
            var service = new FacilityServiceImpl(context);

            // Create a facility first
            var facilityRequest = new CreateFacilityRequest
            {
                Name = "Central Warehouse",
                Type = "Warehouse",
                Region = "West"
            };
            await service.CreateFacilityAsync(facilityRequest);

            // Get the created facility to use its ID
            var facility = await context.Facilities.FirstOrDefaultAsync(f => f.Name == "Central Warehouse");
            Assert.NotNull(facility);

            var zoneRequest = new CreateStorageZoneRequest
            {
                FacilityId = facility.FacilityId,
                Name = "Deep Freeze Zone",
                TemperatureProfile = "Freezer",
                Capacity = 2000.75m
            };

            // Act
            var result = await service.CreateZoneAsync(zoneRequest);

            // Assert
            Assert.True(result);

            // Verify zone was created
            var zone = await context.StorageZones
                .FirstOrDefaultAsync(z => z.Name == "Deep Freeze Zone" && z.FacilityId == facility.FacilityId);
            Assert.NotNull(zone);
            Assert.Equal("Deep Freeze Zone", zone.Name);
            Assert.Equal("Freezer", zone.TemperatureProfile);
            Assert.Equal(2000.75m, zone.Capacity);
            Assert.Equal(facility.FacilityId, zone.FacilityId);
        }
    }
}