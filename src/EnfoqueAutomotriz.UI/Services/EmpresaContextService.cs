using Microsoft.Extensions.Caching.Memory;

namespace EnfoqueAutomotriz.UI.Services;

public class EmpresaContextService(IApiService apiService, IMemoryCache cache) : IEmpresaContextService
{
    private const string CacheKey = "empresa_context";

    private class ConfiguracionEmpresaDto
    {
        public string? NombreComercial { get; set; }
        public string? RazonSocial { get; set; }
        public string? LogoUrl { get; set; }
    }

    public async Task<EmpresaContext> GetAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out EmpresaContext? cached) && cached is not null)
            return cached;

        var (data, _) = await apiService.GetAsync<ConfiguracionEmpresaDto>("api/v1/configuracion-empresa", ct);

        var ctx = new EmpresaContext(
            NombreComercial: data?.NombreComercial ?? data?.RazonSocial ?? "EnfoqueAutomotriz",
            LogoUrl: data?.LogoUrl,
            RazonSocial: data?.RazonSocial
        );

        cache.Set(CacheKey, ctx, TimeSpan.FromMinutes(30));
        return ctx;
    }

    public void Invalidar() => cache.Remove(CacheKey);
}
