using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace B2C.Infrastructure.Outbox.Dispatchers
{
    /// <summary>
    /// Диспетчер исходящих событий B2C → B2B. Destination = "b2b".
    /// 
    /// Аутентификация — X-Service-Key (межсервисная, не JWT покупателя).
    /// 
    /// NB: на данный момент B2C не эмитит async-события наружу (IntegrationEventMapper
    /// возвращает пусто), поэтому этот диспетчер фактически не вызывается. Он готов
    /// к будущим событиям (например, уведомления о статусе заказа для B2B-аналитики).
    /// 
    /// Endpoint берётся из EventType→route-маппинга. Сейчас — единый generic endpoint
    /// приёма событий B2B; при появлении конкретных событий уточним маршрутизацию.
    /// </summary>
    public sealed class HttpB2bDispatcher : IIntegrationEventDispatcher
    {
        private readonly HttpClient _httpClient;
        private readonly B2BClientOptions _options;
        private readonly ILogger<HttpB2bDispatcher> _logger;

        public HttpB2bDispatcher(
            HttpClient httpClient,
            IOptions<B2BClientOptions> options,
            ILogger<HttpB2bDispatcher> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public string Destination => "b2b";

        public async Task SendAsync(OutboxMessage message, CancellationToken ct)
        {
            // Generic endpoint приёма событий на стороне B2B.
            const string endpoint = "/api/v1/events/b2c";

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(message.Payload, Encoding.UTF8, "application/json"),
            };

            request.Headers.Add("X-Service-Key", _options.ServiceKey);

            _logger.LogInformation(
                "Sending {EventType} (id={MessageId}) to B2B",
                message.EventType, message.Id);

            var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException(
                    $"B2B rejected {message.EventType}: " +
                    $"HTTP {(int)response.StatusCode} {response.StatusCode} — {body}");
            }
        }
    }
}
