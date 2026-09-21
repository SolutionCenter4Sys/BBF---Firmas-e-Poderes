using System.Security.Cryptography;
using System.Text;
using BbfFirmasPoderes.Domain.Auth;
using BbfFirmasPoderes.Domain.Correlation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BbfFirmasPoderes.Api.Auth;

public static class AuthEndpoints
{
    public static WebApplication MapAuth(this WebApplication app)
    {
        app.MapPost("/v1/auth/login", Login)
            .AllowAnonymous()
            .WithTags("Auth")
            .WithSummary("Login demo (operador / auditor)")
            .WithDescription("Emite JWT HS256. Senhas só em env DemoUsers__*Password — nunca no código.");

        return app;
    }

    private static IResult Login(
        HttpContext http,
        LoginRequest? body,
        IOptions<DemoUsersOptions> demoOptions,
        DemoJwtIssuer issuer)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
        {
            return Problem(
                http,
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                "Informe email e password.");
        }

        var demo = demoOptions.Value;
        if (!demo.HasAnyPassword())
        {
            return Problem(
                http,
                StatusCodes.Status503ServiceUnavailable,
                "Service Unavailable",
                "https://tools.ietf.org/html/rfc9110#section-15.6.4",
                "Usuários demo não configurados. Defina DemoUsers__OperadorPassword e DemoUsers__AuditorPassword.");
        }

        var match = demo.Match(body.Email, body.Password);
        if (match is null)
        {
            return Problem(
                http,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                "Credenciais inválidas.");
        }

        var hours = demo.TokenLifetimeHours > 0 ? demo.TokenLifetimeHours : 8;
        var lifetime = TimeSpan.FromHours(hours);
        var token = issuer.Issue(match.Value.Email, match.Value.Role, lifetime);

        return Results.Json(new LoginResponse(
            token,
            "Bearer",
            (int)lifetime.TotalSeconds,
            match.Value.Role,
            match.Value.Email));
    }

    private static IResult Problem(HttpContext http, int status, string title, string type, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = http.Request.Path
        };
        problem.Extensions["correlationId"] = CorrelationContext.Current
            ?? http.Items[CorrelationIds.HeaderName]?.ToString();
        return Results.Json(problem, statusCode: status, contentType: "application/problem+json");
    }
}

public sealed record LoginRequest(string? Email, string? Password);

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string Role,
    string Email);

internal static class DemoUsersOptionsExtensions
{
    public static bool HasAnyPassword(this DemoUsersOptions options) =>
        !string.IsNullOrEmpty(options.OperadorPassword) || !string.IsNullOrEmpty(options.AuditorPassword);

    public static (string Email, string Role)? Match(this DemoUsersOptions options, string email, string password)
    {
        if (EmailEquals(email, options.OperadorEmail) && PasswordEquals(password, options.OperadorPassword))
            return (options.OperadorEmail, Roles.Operador);

        if (EmailEquals(email, options.AuditorEmail) && PasswordEquals(password, options.AuditorPassword))
            return (options.AuditorEmail, Roles.Auditor);

        return null;
    }

    private static bool EmailEquals(string left, string right) =>
        !string.IsNullOrWhiteSpace(right)
        && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool PasswordEquals(string provided, string expected)
    {
        if (string.IsNullOrEmpty(expected))
            return false;

        var left = Encoding.UTF8.GetBytes(provided);
        var right = Encoding.UTF8.GetBytes(expected);
        if (left.Length != right.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(left, right);
    }
}
