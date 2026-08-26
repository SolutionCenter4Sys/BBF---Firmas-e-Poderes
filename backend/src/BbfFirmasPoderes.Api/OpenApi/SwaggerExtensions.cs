using Microsoft.OpenApi.Models;

namespace BbfFirmasPoderes.Api.OpenApi;

public static class SwaggerExtensions
{
    public static IServiceCollection AddBbfSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "BBF Firmas e Poderes",
                Version = "v1",
                Description = "Upload assíncrono /v1/documents (202 + outbox). Sem OCR/KAAS neste recorte."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Bearer. Perfis: operador, auditor, admin, consumer.",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static WebApplication UseBbfSwagger(this WebApplication app)
    {
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "swagger/{documentName}/swagger.json";
        });
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "BBF Firmas e Poderes v1");
            options.RoutePrefix = "swagger";
        });

        return app;
    }
}
