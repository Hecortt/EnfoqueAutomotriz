using EnfoqueAutomotriz.Api.Repository;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Api.Services;

namespace EnfoqueAutomotriz.Api.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITenantLimitService, TenantLimitService>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IAccesoLogRepository, AccesoLogRepository>();
        services.AddScoped<IPermisoRolRepository, PermisoRolRepository>();
        services.AddScoped<INotificacionRepository, NotificacionRepository>();
        services.AddScoped<ICatalogoRepository, CatalogoRepository>();
        services.AddScoped<IConfiguracionEmpresaRepository, ConfiguracionEmpresaRepository>();
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();

        // Taller
        services.AddScoped<ISucursalRepository, SucursalRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IOrdenServicioRepository, OrdenServicioRepository>();
        services.AddScoped<IAlmacenRepository, AlmacenRepository>();
        services.AddScoped<IComprasRepository, ComprasRepository>();
        services.AddScoped<IPromocionRepository, PromocionRepository>();
        services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
        services.AddScoped<IComunicacionRepository, ComunicacionRepository>();
        return services;
    }
}
