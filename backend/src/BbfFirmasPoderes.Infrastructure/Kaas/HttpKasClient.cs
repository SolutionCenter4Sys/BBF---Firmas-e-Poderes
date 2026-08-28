using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BbfFirmasPoderes.Domain.Kaas;
using Microsoft.Extensions.Options;

namespace BbfFirmasPoderes.Infrastructure.Kaas;

public sealed class HttpKasClient(HttpClient http, IOptions<KasOptions> options) : IKasClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<KasCallResult> PostAsync(object envelope, CancellationToken cancellationToken = default)
    {
        var kas = options.Value;
        if (string.IsNullOrWhiteSpace(kas.ApiKey))
        {
            throw new InvalidOperationException(
                "Kas:ApiKey ausente. Defina Kas__ApiKey apenas no Worker. A chave não passa pelo Next.js.");
        }

        var url = string.IsNullOrWhiteSpace(kas.RunUrl) ? KasDefaults.DefaultRunUrl : kas.RunUrl;
        var json = JsonSerializer.Serialize(envelope, JsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation(KasDefaults.ApiKeyHeader, kas.ApiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var started = Stopwatch.StartNew();
        using var response = await http.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        started.Stop();

        JsonElement? parsed = null;
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                parsed = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                parsed = null;
            }
        }

        return new KasCallResult(
            response.IsSuccessStatusCode,
            (int)response.StatusCode,
            response.ReasonPhrase ?? response.StatusCode.ToString(),
            raw,
            parsed,
            (int)started.ElapsedMilliseconds);
    }
}
