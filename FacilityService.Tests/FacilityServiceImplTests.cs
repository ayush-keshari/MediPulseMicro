using Xunit;
using FacilityService.Models;

namespace FacilityService.Tests;

public class FacilityServiceTests
{
    [Fact]
    public void Facility_HasName()
    {
        var facility = new Facility { Name = "Apollo Hospital" };
        Assert.Equal("Apollo Hospital", facility.Name);
    }

    [Fact]
    public void Facility_HasType()
    {
        var facility = new Facility { Type = "Hospital" };
        Assert.Equal("Hospital", facility.Type);
    }

    [Fact]
    public void StorageZone_HasName()
    {
        var zone = new StorageZone { Name = "Cold Storage A1" };
        Assert.Equal("Cold Storage A1", zone.Name);
    }

    [Fact]
    public void StorageZone_HasCapacity()
    {
        var zone = new StorageZone { Capacity = 5000m };
        Assert.Equal(5000m, zone.Capacity);
    }
}