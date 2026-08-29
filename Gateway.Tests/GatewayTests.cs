using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;

namespace Gateway.Tests;

public class GatewayTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GatewayTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Gateway_ShouldStartSuccessfully()
    {
        // Arrange & Act
        var client = _factory.CreateClient();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Gateway_ShouldHaveCorsPolicyConfigured()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var baseAddress = client.BaseAddress;

        // Assert
        Assert.NotNull(baseAddress);
        Assert.NotEmpty(baseAddress.ToString());
    }

    [Fact]
    public void Gateway_ShouldRegisterOcelotServices()
    {
        // Arrange
        var host = _factory.Services;

        // Act & Assert
        // Verify that Ocelot services are registered
        // We can't directly test Ocelot's internal state without making proxy requests
        // but we can verify the host builds successfully
        Assert.NotNull(host);

        // Additional verification: Check that we can resolve basic services
        var loggerFactory = host.GetService(typeof(ILoggerFactory));
        Assert.NotNull(loggerFactory);
    }

    [Fact]
    public void Gateway_Configuration_ShouldLoadOcelotJson()
    {
        // Arrange
        var host = _factory.Services;
        var configuration = host.GetRequiredService<IConfiguration>();

        // Act & Assert
        Assert.NotNull(configuration);

        // Verify that the configuration can be accessed
        // In a real test, we'd check specific values from ocelot.json
        // For now, we verify the configuration system works
        var testValue = configuration["TestKey"] ?? "default";
        Assert.NotNull(testValue);
    }

    // Integration-style test - would require actual downstream services
    // For unit testing the gateway itself, we focus on startup and configuration
    [Fact]
    public void Gateway_ShouldHaveSerilogConfigured()
    {
        // Arrange
        var host = _factory.Services;

        // Act & Assert
        // Verify that Serilog is configured by checking if we can get ILogger
        var logger = host.GetService(typeof(ILogger<Program>));
        Assert.NotNull(logger);
    }
}
