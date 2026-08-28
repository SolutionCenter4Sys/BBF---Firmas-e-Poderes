using System.Text.Json;

namespace BbfFirmasPoderes.Domain.Kaas;

public sealed record KasCallResult(
    bool Ok,
    int HttpStatus,
    string StatusText,
    string RawBody,
    JsonElement? Parsed,
    int DurationMs);
