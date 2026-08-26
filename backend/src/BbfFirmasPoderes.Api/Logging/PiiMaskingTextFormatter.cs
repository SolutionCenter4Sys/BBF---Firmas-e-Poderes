using BbfFirmasPoderes.Domain.Pii;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace BbfFirmasPoderes.Api.Logging;

internal sealed class PiiMaskingTextFormatter : ITextFormatter
{
    private readonly MessageTemplateTextFormatter _inner = new(
        "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}");

    public void Format(LogEvent logEvent, TextWriter output)
    {
        using var buffer = new StringWriter();
        _inner.Format(logEvent, buffer);
        output.Write(PiiMask.InText(buffer.ToString()));
    }
}
