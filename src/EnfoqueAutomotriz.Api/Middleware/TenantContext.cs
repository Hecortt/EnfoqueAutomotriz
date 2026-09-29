namespace EnfoqueAutomotriz.Api.Middleware;

public interface ITenantContext
{
    string Slug { get; }
    string SchemaName { get; }
    bool IsResolved { get; }
}

public sealed class TenantContext : ITenantContext
{
    public string Slug { get; private set; } = "public";
    public string SchemaName { get; private set; } = "public";
    public bool IsResolved { get; private set; }

    public void Set(string slug, string schemaName)
    {
        Slug = slug;
        SchemaName = schemaName;
        IsResolved = true;
    }
}
