namespace EnfoqueAutomotriz.UI.Services;

public interface IFeatureFlagService
{
    Task<List<string>> GetActivosAsync();
}

public class FeatureFlagService : IFeatureFlagService
{
    private readonly IApiService _api;
    public FeatureFlagService(IApiService api) => _api = api;

    public async Task<List<string>> GetActivosAsync()
    {
        var (features, _) = await _api.GetAsync<List<string>>("api/v1/feature-flags");
        return features ?? [];
    }
}
