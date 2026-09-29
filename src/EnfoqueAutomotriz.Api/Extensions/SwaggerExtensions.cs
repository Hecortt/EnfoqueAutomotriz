using Asp.Versioning;
using Microsoft.OpenApi.Models;

namespace EnfoqueAutomotriz.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(o =>
        {
            o.DefaultApiVersion = new ApiVersion(1, 0);
            o.AssumeDefaultVersionWhenUnspecified = true;
            o.ReportApiVersions = true;
        })
        .AddApiExplorer(o =>
        {
            o.GroupNameFormat = "'v'VVV";
            o.SubstituteApiVersionInUrl = true;
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "EnfoqueAutomotriz API",
                Version = "v1",
                Description = "Plataforma SaaS multi-tenant para talleres automotrices — API"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Token JWT — introduce solo el token sin el prefijo 'Bearer'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });

            c.CustomSchemaIds(UniqueSchemaId);
            c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
        });

        return services;
    }

    public static WebApplication UseSwaggerWithVersioning(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "EnfoqueAutomotriz API v1");
            c.RoutePrefix = "swagger";
        });
        return app;
    }

    private static string UniqueSchemaId(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return $"Nullable{UniqueSchemaId(underlying)}";

        if (!type.IsGenericType)
            return SanitizeName(type.Name);

        var baseName = type.Name.Split('`')[0];
        var argNames = string.Join("And", type.GetGenericArguments().Select(UniqueSchemaId));
        return $"{SanitizeName(baseName)}Of{argNames}";
    }

    private static string SanitizeName(string name) =>
        name.Replace("`", "").Replace("[", "").Replace("]", "").Replace(",", "").Replace(" ", "");
}
