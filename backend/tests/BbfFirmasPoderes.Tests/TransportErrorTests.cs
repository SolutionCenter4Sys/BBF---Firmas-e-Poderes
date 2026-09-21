using System.Net.Sockets;
using BbfFirmasPoderes.Domain.Kaas;

namespace BbfFirmasPoderes.Tests;

public class TransportErrorTests
{
    [Fact]
    public void IsTransient_SocketReset_WithoutHttp_IsTrue()
    {
        var ex = new SocketException((int)SocketError.ConnectionReset);
        Assert.True(TransportError.IsTransient(ex, lastCall: null));
    }

    [Fact]
    public void IsTransient_AfterHttp200_IsFalse()
    {
        var lastCall = new KasCallResult(true, 200, "OK", "{}", null, 10);
        var ex = new SocketException((int)SocketError.ConnectionReset);
        Assert.False(TransportError.IsTransient(ex, lastCall));
    }

    [Fact]
    public void IsTransient_AfterAnyHttpStatus_IsFalse()
    {
        var lastCall = new KasCallResult(false, 502, "Bad Gateway", "{}", null, 10);
        var ex = new HttpRequestException("reset");
        Assert.False(TransportError.IsTransient(ex, lastCall));
    }
}
