using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BbfFirmasPoderes.Domain.Kaas;
using Microsoft.Extensions.Options;

namespace BbfFirmasPoderes.Infrastructure.Kaas;

public sealed class HttpKasClient(HttpClient http, IOptions<KasOptions> options) : IKasClient
{
    public async Task<KasCallResult> PostDocumentAsync(
        Stream document,
        string fileName,
        string contentType,
        string fileField,
        CancellationToken cancellationToken = default)
    {
        var kas = options.Value;
        if (string.IsNullOrWhiteSpace(kas.ApiKey))
        {
            throw new InvalidOperationException(
                "Kas:ApiKey ausente. Defina Kas__ApiKey apenas no Worker. A chave não passa pelo Next.js.");
        }

        var url = string.IsNullOrWhiteSpace(kas.RunUrl) ? KasDefaults.DefaultRunUrl : kas.RunUrl;

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Version = HttpVersion.Version20;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        request.Headers.TryAddWithoutValidation(KasDefaults.ApiKeyHeader, kas.ApiKey);
        using var multipart = new MultipartFormDataContent();
        using var payload = new StringContent("{}", Encoding.UTF8, "application/json");
        multipart.Add(new StringContent(KasDefaults.ModeSync), "mode");
        multipart.Add(payload, "payload");

        var file = new StreamContent(document);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        multipart.Add(file, fileField, fileName);
        request.Content = multipart;
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
