using System.Data.Common;
using EnfoqueAutomotriz.Api.Middleware;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnfoqueAutomotriz.Api.Context;

/// <summary>
/// Interceptor de conexión Npgsql: al abrirse la conexión, fija el search_path
/// al schema del tenant (taller) activo resuelto por TenantResolutionMiddleware.
/// Este es el mecanismo central de aislamiento de datos schema-per-tenant.
/// </summary>
public sealed class TenantSchemaInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContext _tenant;

    public TenantSchemaInterceptor(ITenantContext tenant)
    {
        _tenant = tenant;
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (_tenant.IsResolved && !string.IsNullOrEmpty(_tenant.SchemaName))
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SET search_path = \"{_tenant.SchemaName}\", public";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public override void ConnectionOpened(
        DbConnection connection,
        ConnectionEndEventData eventData)
    {
        if (_tenant.IsResolved && !string.IsNullOrEmpty(_tenant.SchemaName))
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SET search_path = \"{_tenant.SchemaName}\", public";
            cmd.ExecuteNonQuery();
        }
    }
}
