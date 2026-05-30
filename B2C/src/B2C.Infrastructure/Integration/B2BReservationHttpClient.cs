using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Integration.Dtos;
using B2C.Infrastructure.Integration.Contracts;
using Microsoft.Extensions.Logging;

namespace B2C.Infrastructure.Integration
{
    public sealed class B2BReservationHttpClient : IB2BReservationClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<B2BReservationHttpClient> _logger;

        public B2BReservationHttpClient(HttpClient http, ILogger<B2BReservationHttpClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<ReserveResult> ReserveAsync(
            Guid idempotencyKey, IReadOnlyList<ReserveLine> items, CancellationToken ct)
        {
            var request = new B2bReserveRequest
            {
                IdempotencyKey = idempotencyKey,
                Items = items.Select(i => new B2bReserveLine { SkuId = i.SkuId, Quantity = i.Quantity }).ToList(),
            };

            var response = await _http.PostAsJsonAsync("/api/v1/reserve", request, ct);

            // 409 от B2B = reserve неуспешен с детализацией (не исключение, это ожидаемый кейс).
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var conflict = await response.Content.ReadFromJsonAsync<B2bReserveResponse>(ct)
                    ?? new B2bReserveResponse { Success = false };
                return B2BHttpMapper.ToReserveResult(conflict);
            }

            response.EnsureSuccessStatusCode();

            var ok = await response.Content.ReadFromJsonAsync<B2bReserveResponse>(ct)
                ?? new B2bReserveResponse { Success = true };
            return B2BHttpMapper.ToReserveResult(ok);
        }

        public async Task<bool> UnreserveAsync(
            Guid idempotencyKey, IReadOnlyList<ReserveLine> items, CancellationToken ct)
        {
            var request = new B2bReserveRequest
            {
                IdempotencyKey = idempotencyKey,
                Items = items.Select(i => new B2bReserveLine { SkuId = i.SkuId, Quantity = i.Quantity }).ToList(),
            };

            var response = await _http.PostAsJsonAsync("/api/v1/unreserve", request, ct);

            // Не бросаем на не-2xx: возвращаем false, handler уйдёт в CANCEL_PENDING.
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Unreserve returned {Status} for key {Key}",
                    (int)response.StatusCode, idempotencyKey);
                return false;
            }

            return true;
        }

        public async Task<bool> FulfillAsync(
            Guid orderId, IReadOnlyList<ReserveLine> items, CancellationToken ct)
        {
            var request = new B2bFulfillRequest
            {
                OrderId = orderId,
                Items = items.Select(i => new B2bReserveLine { SkuId = i.SkuId, Quantity = i.Quantity }).ToList(),
            };

            var response = await _http.PostAsJsonAsync("/api/v1/fulfill", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Fulfill returned {Status} for order {OrderId}",
                    (int)response.StatusCode, orderId);
                return false;
            }

            return true;
        }
    }
}
