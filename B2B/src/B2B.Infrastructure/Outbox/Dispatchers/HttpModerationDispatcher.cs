using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace B2B.Infrastructure.Outbox.Dispatchers
{
    public sealed class HttpModerationDispatcher : IIntegrationEventDispatcher
    {
        private readonly HttpClient _httpClient;
        private readonly ModerationClientOptions _options;
        private readonly ILogger<HttpModerationDispatcher> _logger;

        public HttpModerationDispatcher(
            HttpClient httpClient,
            IOptions<ModerationClientOptions> options,
            ILogger<HttpModerationDispatcher> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public string Destination => "moderation";

        public async Task SendAsync(OutboxMessage message, CancellationToken ct)
        {
            const string endpoint = "/api/v1/events/product";

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(
                    message.Payload,
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            request.Headers.Add("X-Service-Key", _options.ServiceKey);

            _logger.LogInformation(
                "Sending {EventType} (id={MessageId}) to Moderation",
                message.EventType, message.Id);

            var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException(
                    $"Moderation rejected {message.EventType}: " +
                    $"HTTP {(int)response.StatusCode} {response.StatusCode} — {body}");
            }
        }
    }


}
