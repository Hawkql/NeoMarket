using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace B2B.Infrastructure.Outbox.Dispatchers
{
    public sealed class HttpB2cDispatcher : IIntegrationEventDispatcher
    {
        private readonly HttpClient _httpClient;
        private readonly B2cClientOptions _options;
        private readonly ILogger<HttpB2cDispatcher> _logger;

        public HttpB2cDispatcher(
            HttpClient httpClient,
            IOptions<B2cClientOptions> options,
            ILogger<HttpB2cDispatcher> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public string Destination => "b2c";

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
                "Sending {EventType} (id={MessageId}) to B2C",
                message.EventType, message.Id);

            var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException(
                    $"B2C rejected {message.EventType}: " +
                    $"HTTP {(int)response.StatusCode} {response.StatusCode} — {body}");
            }
        }
    }
}
