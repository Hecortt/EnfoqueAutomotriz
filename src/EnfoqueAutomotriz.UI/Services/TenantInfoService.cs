namespace EnfoqueAutomotriz.UI.Services;

public record TenantInfo(string Slug, string Plan, string NombreComercial);

public interface ITenantInfoService
{
    Task<TenantInfo> GetAsync();
}

public class TenantInfoService : ITenantInfoService
{
    private readonly IApiService _api;
    public TenantInfoService(IApiService api) => _api = api;

    private class TenantInfoDto
    {
        public string Slug { get; set; } = "";
        public string Plan { get; set; } = "";
        public string NombreComercial { get; set; } = "";
    }

    public async Task<TenantInfo> GetAsync()
    {
        var (data, _) = await _api.GetAsync<TenantInfoDto>("api/v1/tenant-info");
        return new TenantInfo(data?.Slug ?? "", data?.Plan ?? "", data?.NombreComercial ?? "");
    }
}
