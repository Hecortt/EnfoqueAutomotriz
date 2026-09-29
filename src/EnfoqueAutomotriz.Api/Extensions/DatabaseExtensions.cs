using EnfoqueAutomotriz.Api.Context;
using EnfoqueAutomotriz.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnfoqueAutomotriz.Api.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config, bool isDevelopment)
    {
        // TenantContext: Scoped (una instancia por request)
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        // TenantSchemaInterceptor: Scoped (necesita ITenantContext del mismo scope)
        services.AddScoped<TenantSchemaInterceptor>();

        // Construir connection string programáticamente para manejar passwords con caracteres especiales
        var connStr = BuildConnectionString(config);

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<TenantSchemaInterceptor>();
            var opts = options.UseNpgsql(connStr, npgsql => npgsql.CommandTimeout(60))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(interceptor);

            if (isDevelopment)
            {
                opts.EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
            }
        });

        return services;
    }

    public static string BuildConnectionString(IConfiguration config)
    {
        var host = config["DatabaseSettings:Host"];
        if (!string.IsNullOrEmpty(host))
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = int.TryParse(config["DatabaseSettings:Port"], out var p) ? p : 5432,
                Database = config["DatabaseSettings:Database"] ?? "enfoqueautomotriz",
                Username = config["DatabaseSettings:Username"] ?? "postgres",
                Password = config["DatabaseSettings:Password"] ?? "",
                CommandTimeout = 60
            };
            return builder.ConnectionString;
        }

        return config.GetConnectionString("DefaultConnection") ?? "";
    }
}
