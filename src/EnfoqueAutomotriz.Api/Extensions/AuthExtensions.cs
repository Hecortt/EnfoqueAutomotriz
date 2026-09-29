using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace EnfoqueAutomotriz.Api.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration config)
    {
        var secret = config.GetValue<string>("ApiSettings:Secret")!;
        var issuer = config.GetValue<string>("ApiSettings:Issuer");
        var audience = config.GetValue<string>("ApiSettings:Audience");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.RequireHttpsMetadata = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
                    ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
                    ValidIssuer = issuer,
                    ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization(options =>
        {
            // Roles del taller automotriz:
            // 0=SuperAdmin SaaS, 1=Admin Taller, 2=Mecánico, 3=Recepcionista,
            // 4=Enfermería (reservado/no usado), 5=Facturación/Contador, 6=Soporte IT

            // Admin Taller (1) y Soporte IT (6) tienen acceso total a la administración del tenant.
            options.AddPolicy("AccesoTotal", policy => policy.RequireRole("0", "1", "6"));

            // Únicamente Admin Taller (1) y Soporte IT (6) gestionan usuarios y ven SQL Console.
            options.AddPolicy("SoloAdminTaller", policy => policy.RequireRole("1", "6"));

            // Únicamente Soporte IT (6) puede ver y ejecutar SQL Console.
            options.AddPolicy("SoloSoporteIT", policy => policy.RequireRole("6"));

            // Administrador de Permisos: Admin Taller (1) y Soporte IT (6)
            options.AddPolicy("AdminPermisos", policy => policy.RequireRole("1", "6"));

            // Mecánico / Técnico (2)
            options.AddPolicy("Mecanico", policy => policy.RequireRole("1", "2", "6"));

            // Recepcionista / Asesor de servicio (3)
            options.AddPolicy("Recepcionista", policy => policy.RequireRole("1", "3", "6"));

            // Facturación / Contador (5)
            options.AddPolicy("Facturacion", policy => policy.RequireRole("1", "5", "6"));

            // Super Admin (0) — Administración SaaS central (tenants, planes, pagos)
            options.AddPolicy("SaasAdmin", policy => policy.RequireRole("0"));
        });
        return services;
    }
}
