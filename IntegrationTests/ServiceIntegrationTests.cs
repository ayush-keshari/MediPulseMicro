using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace IntegrationTests;

public class ServiceIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _gatewayClient;

    public ServiceIntegrationTests(IntegrationTestFixture fixture)
    {
        _gatewayClient = fixture.GatewayClient;
    }

    [Fact]
    public async Task Gateway_ShouldBeAccessible()
    {
        // Act
        var response = await _gatewayClient.GetAsync("/");

        // Assert
        Assert.NotNull(response);
        // Gateway might return 404 for root path, but should not fail to connect
        Assert.NotEqual(HttpStatusCode.RequestTimeout, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task AuthService_Endpoint_ShouldBeAccessible_Via_Gateway()
    {
        // Act
        var response = await _gatewayClient.GetAsync("auth");

        // Assert
        Assert.NotNull(response);
        // Should not timeout or fail to connect
        Assert.NotEqual(HttpStatusCode.RequestTimeout, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        // Might return 404 if endpoint doesn't exist, or 401 if auth required, but that means gateway is working
    }

    [Fact]
    public async Task FacilityService_Endpoint_ShouldBeAccessible_Via_Gateway()
    {
        // Act
        var response = await _gatewayClient.GetAsync("facility");

        // Assert
        Assert.NotNull(response);
        // Should not timeout or fail to connect
        Assert.NotEqual(HttpStatusCode.RequestTimeout, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}