using System.Text;
using System.Text.Json;

namespace Shared.Helpers;

// Fire-and-forget helper that POSTs a notification to NotificationService.
// Call from any controller action after a successful mutating operation.
// Failures are swallowed — a downed NotificationService must never block
// operational requests.
//
// To enable, add to the service's appsettings.json:
//   "NotificationService": { "BaseUrl": "http://localhost:5007" }
public static class NotificationClient
{
    public static void Send(
        IHttpClientFactory factory,
        string? baseUrl,
        string? bearerToken,
        string userId,
        string category,
        string title,
        string message)
    {
        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(userId)) return;

        _ = Task.Run(async () =>
        {
            try
            {
                var payload = JsonSerializer.Serialize(new { userId, category, title, message });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var client = factory.CreateClient();
                if (!string.IsNullOrEmpty(bearerToken))
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", bearerToken);
                await client.PostAsync($"{baseUrl.TrimEnd('/')}/api/notifications", content);
            }
            catch
            {
                // Swallow — a downed NotificationService must never affect operational requests.
            }
        });
    }
}
