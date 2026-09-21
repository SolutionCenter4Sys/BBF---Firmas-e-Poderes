using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using BbfFirmasPoderes.Api.Auth;
using BbfFirmasPoderes.Domain.Auth;
using Microsoft.IdentityModel.Tokens;

namespace BbfFirmasPoderes.Tests;

internal static class TestAuth
{
    public const string Issuer = "https://bbf.test";
    public const string Audience = "bbf-firmas-poderes-test";
    public const string SigningKey = "test-only-signing-key-32-bytes!!";

    public const string DemoOperadorEmail = "ana.silva@bbf.com.br";
    public const string DemoOperadorPassword = "operador-test-pass";
    public const string DemoAuditorEmail = "auditor.interno@bbf.com.br";
    public const string DemoAuditorPassword = "auditor-test-pass";

    public static string CreateToken(string role, string? sub = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var subject = sub ?? $"user-{role}";

        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, subject),
                new Claim(ClaimTypes.Role, role)
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static void Bearer(this HttpClient client, string role)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(role));
    }

    public static JwtOptions Options => new()
    {
        Issuer = Issuer,
        Audience = Audience,
        SigningKey = SigningKey
    };
}
