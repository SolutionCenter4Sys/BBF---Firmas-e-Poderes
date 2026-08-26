using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BbfFirmasPoderes.Domain.Audit;
using BbfFirmasPoderes.Domain.Auth;
using BbfFirmasPoderes.Domain.Correlation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace BbfFirmasPoderes.Api.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddBbfAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "Jwt não configurado. Defina Jwt__Issuer, Jwt__Audience e Jwt__SigningKey.");

        if (string.IsNullOrWhiteSpace(jwt.Issuer)
            || string.IsNullOrWhiteSpace(jwt.Audience)
            || string.IsNullOrWhiteSpace(jwt.SigningKey))
        {
            throw new InvalidOperationException(
                "Jwt:Issuer, Jwt:Audience e Jwt:SigningKey são obrigatórios (env Jwt__*).");
        }

        if (Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey deve ter no mínimo 32 bytes (HS256).");
        }

        services.AddSingleton(jwt);
        services.AddHttpContextAccessor();
        services.Replace(ServiceDescriptor.Scoped<IAuditContext, HttpAuditContext>());

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = JwtRegisteredClaimNames.Sub
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal is not null)
                            Roles.NormalizeClaims(context.Principal);
                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        if (context.Response.HasStarted)
                            return;

                        await WriteProblemAsync(
                            context.HttpContext,
                            StatusCodes.Status401Unauthorized,
                            "Unauthorized",
                            "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                            "Token ausente ou inválido.");
                    },
                    OnForbidden = async context =>
                    {
                        if (context.Response.HasStarted)
                            return;

                        await WriteProblemAsync(
                            context.HttpContext,
                            StatusCodes.Status403Forbidden,
                            "Forbidden",
                            "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                            "Perfil sem permissão.");
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(Policies.HealthRead, policy =>
                policy.RequireRole(Roles.Auditor, Roles.Admin));

            options.AddPolicy(Policies.Operador, policy =>
                policy.RequireRole(Roles.Operador, Roles.Admin));

            options.AddPolicy(Policies.Auditor, policy =>
                policy.RequireRole(Roles.Auditor, Roles.Admin));

            options.AddPolicy(Policies.Admin, policy =>
                policy.RequireRole(Roles.Admin));

            options.AddPolicy(Policies.Consumer, policy =>
                policy.RequireRole(Roles.Consumer, Roles.Admin));

            options.AddPolicy(Policies.DocumentsUpload, policy =>
                policy.RequireRole(Roles.Operador, Roles.Consumer, Roles.Admin));

            options.AddPolicy(Policies.DocumentsRead, policy =>
                policy.RequireRole(Roles.Operador, Roles.Auditor, Roles.Consumer, Roles.Admin));
        });

        return services;
    }

    private static Task WriteProblemAsync(
        HttpContext http,
        int status,
        string title,
        string type,
        string detail)
    {
        http.Response.StatusCode = status;

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

        return http.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
}
