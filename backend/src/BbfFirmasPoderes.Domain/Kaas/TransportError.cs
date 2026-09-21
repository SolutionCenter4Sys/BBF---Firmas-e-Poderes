using System.Net.Sockets;

namespace BbfFirmasPoderes.Domain.Kaas;

/// <summary>
/// Falha de transporte antes de um HTTP do KAAS. Não reenvia se já houve 200.
/// </summary>
public static class TransportError
{
    public static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(45),
        TimeSpan.FromSeconds(90)
    ];

    public const int MaxAttempts = 4;

    public static bool IsTransient(Exception ex, KasCallResult? lastCall)
    {
        if (lastCall is { Ok: true })
            return false;

        if (lastCall is { HttpStatus: > 0 })
            return false;

        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is SocketException socket
                && socket.SocketErrorCode is SocketError.ConnectionReset
                    or SocketError.TimedOut
                    or SocketError.NetworkReset
                    or SocketError.HostUnreachable
                    or SocketError.TryAgain)
            {
                return true;
            }

            if (current is HttpRequestException or IOException or TimeoutException)
                return true;

            if (current is TaskCanceledException)
                return true;
        }

        return false;
    }

    public static TimeSpan DelayAfter(int failedAttempt)
    {
        var index = Math.Clamp(failedAttempt - 1, 0, RetryDelays.Length - 1);
        return RetryDelays[index];
    }
}
