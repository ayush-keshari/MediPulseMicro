using System.Net.Http;
using Xunit;

namespace IntegrationTests;

public class IntegrationTestFixture : IDisposable
{
    public HttpClient GatewayClient { get; }

    public IntegrationTestFixture()
    {
        // For local testing, assume services are running on localhost:5000 (gateway port)
        // In CI, this would be set via environment variables or configuration
        var baseUrl = Environment.GetEnvironmentVariable("GATEWAY_BASE_URL") ?? "http://localhost:5000";

        GatewayClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
    }

    public void Dispose()
    {
        GatewayClient.Dispose();
    }
}