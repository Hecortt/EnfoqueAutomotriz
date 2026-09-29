using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using EnfoqueAutomotriz.Api.Extensions;
using EnfoqueAutomotriz.Domain.Shared;
using Npgsql;

namespace EnfoqueAutomotriz.Api.Endpoints;

/// <summary>
/// Administración SaaS central: talleres (tenants), planes, feature flags y pagos.
/// Protegido con la policy "SaasAdmin" (rol 0, fuera de cualquier tenant).
/// </summary>
public static class SaasAdminEndpoints
{
    public static RouteGroupBuilder MapSaasAdminEndpoints(this RouteGroupBuilder api, ApiVersion v1)
    {
        var saas = api.MapGroup("/saas-admin")
            .WithTags("SaaS Admin")
            .RequireAuthorization("SaasAdmin");

        // ── PLANES ──────────────────────────────────────────────────────────────

        saas.MapGet("/planes", async (IConfiguration config, CancellationToken ct) =>
        {
            var connStr = DatabaseExtensions.BuildConnectionString(config);
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, slug, nombre, descripcion, precio_mensual, precio_anual,
                       moneda, max_usuarios, features, activo, orden
                FROM master.planes ORDER BY orden";

            var planes = new List<PlanDto>();
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                planes.Add(new PlanDto(
                    r.GetGuid(0), r.GetString(1), r.GetString(2),
                    r.IsDBNull(3) ? null : r.GetString(3),
                    r.GetDecimal(4), r.GetDecimal(5), r.GetString(6),
                    r.GetInt32(7),
                    r.GetFieldValue<string[]>(8),
                    r.GetBoolean(9), r.GetInt16(10)
                ));
            }
            return Results.Ok(APIResponse<List<PlanDto>>.Success(planes));
        })
        .MapToApiVersion(v1)
        .Produces<APIResponse<List<PlanDto>>>();

        // ── TENANTS (TALLERES): LISTA ────────────────────────────────────────────

        saas.MapGet("/tenants", async (IConfiguration config, CancellationToken ct) =>
        {
            var connStr = DatabaseExtensions.BuildConnectionString(config);
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT t.id, t.slug, t.nombre_comercial, t.plan, t.estado,
                       t.schema_name, t.max_usuarios, t.fecha_vencimiento,
                       t.contacto_email, t.contacto_nombre, t.razon_social, t.rfc,
                       t.created_at, t.updated_at,
                       COUNT(ff.id) FILTER (WHERE ff.habilitado) AS features_activos
                FROM master.tenants t
                LEFT JOIN master.feature_flags ff ON ff.tenant_id = t.id
                GROUP BY t.id
                ORDER BY t.created_at DESC";

            var tenants = new List<TenantListDto>();
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                tenants.Add(new TenantListDto(
                    r.GetGuid(0), r.GetString(1), r.GetString(2), r.GetString(3),
                    r.GetString(4), r.GetString(5), r.GetInt32(6),
                    r.IsDBNull(7) ? null : r.GetDateTime(7),
                    r.IsDBNull(8) ? null : r.GetString(8),
                    r.IsDBNull(9) ? null : r.GetString(9),
                    r.IsDBNull(10) ? null : r.GetString(10),
                    r.IsDBNull(11) ? null : r.GetString(11),
                    r.GetDateTime(12),
                    r.IsDBNull(13) ? null : r.GetDateTime(13),
                    r.GetInt64(14)
                ));
            }
            return Results.Ok(APIResponse<List<TenantListDto>>.Success(tenants));
        })
        .MapToApiVersion(v1)
        .Produces<APIResponse<List<TenantListDto>>>();

        // ── TENANTS: DETALLE ────────────────────────────────────────────────────

        saas.MapGet("/tenants/{slug}", async (
            string slug, IConfiguration config, CancellationToken ct) =>
        {
            var connStr = DatabaseExtensions.BuildConnectionString(config);
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, slug, nombre_comercial, razon_social, rfc, logo_url,
                       plan, estado, schema_name, db_host, max_usuarios,
                       fecha_vencimiento, contacto_email, contacto_nombre, created_at, updated_at
                FROM master.tenants WHERE slug = $1";
            cmd.Parameters.AddWithValue(slug);

            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct))
                return Results.NotFound(APIResponse.Failure($"Taller '{slug}' no encontrado."));

            var tenant = new TenantDetailDto(
                r.GetGuid(0), r.GetString(1), r.GetString(2),
                r.IsDBNull(3) ? null : r.GetString(3),
                r.IsDBNull(4) ? null : r.GetString(4),
                r.IsDBNull(5) ? null : r.GetString(5),
                r.GetString(6), r.GetString(7), r.GetString(8),
                r.GetString(9), r.GetInt32(10),
                r.IsDBNull(11) ? null : r.GetDateTime(11),
                r.IsDBNull(12) ? null : r.GetString(12),
                r.IsDBNull(13) ? null : r.GetString(13),
                r.GetDateTime(14),
                r.IsDBNull(15) ? null : r.GetDateTime(15),
                [], []
            );
            await r.CloseAsync();

            await using var cmd2 = conn.CreateCommand();
            cmd2.CommandText = @"
                SELECT feature, habilitado FROM master.feature_flags
                WHERE tenant_id = $1 ORDER BY feature";
            cmd2.Parameters.AddWithValue(tenant.Id);

            var features = new List<FeatureFlagDto>();
            await using var r2 = await cmd2.ExecuteReaderAsync(ct);
            while (await r2.ReadAsync(ct))
                features.Add(new FeatureFlagDto(r2.GetString(0), r2.GetBoolean(1)));
            await r2.CloseAsync();

            await using var cmd3 = conn.CreateCommand();
            cmd3.CommandText = @"
                SELECT version, aplicado_at FROM master.schema_migrations
                WHERE tenant_id = $1 ORDER BY aplicado_at DESC";
            cmd3.Parameters.AddWithValue(tenant.Id);

            var migrations = new List<MigrationDto>();
            await using var r3 = await cmd3.ExecuteReaderAsync(ct);
            while (await r3.ReadAsync(ct))
                migrations.Add(new MigrationDto(r3.GetString(0), r3.GetDateTime(1)));

            return Results.Ok(APIResponse<TenantDetailDto>.Success(
                tenant with { Features = features, Migraciones = migrations }
            ));
        })
        .MapToApiVersion(v1)
        .Produces<APIResponse<TenantDetailDto>>();

        // ── TENANTS: CREAR + PROVISIONAR ────────────────────────────────────────

        saas.MapPost("/tenants", async (
            TenantCreateDto dto, IConfiguration config, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Slug) || string.IsNullOrWhiteSpace(dto.NombreComercial))
                return Results.BadRequest(APIResponse.Failure("Slug y nombre comercial son requeridos."));

            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Slug, @"^[a-z0-9_]{2,50}$"))
                return Results.BadRequest(APIResponse.Failure("El slug solo acepta letras minúsculas, números y guiones bajos (2-50 chars)."));

            var connStr = DatabaseExtensions.BuildConnectionString(config);
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            await using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(1) FROM master.tenants WHERE slug = $1";
            checkCmd.Parameters.AddWithValue(dto.Slug);
            var exists = (long)(await checkCmd.ExecuteScalarAsync(ct))! > 0;
            if (exists)
                return Results.Conflict(APIResponse.Failure($"Ya existe un taller con slug '{dto.Slug}'."));

            string[] planFeatures = [];
            int maxUsuarios = 5;
            await using var planCmd = conn.CreateCommand();
            planCmd.CommandText = "SELECT features, max_usuarios FROM master.planes WHERE slug = $1";
            planCmd.Parameters.AddWithValue(dto.Plan ?? "basico");
            await using var planReader = await planCmd.ExecuteReaderAsync(ct);
            if (await planReader.ReadAsync(ct))
            {
                planFeatures = planReader.GetFieldValue<string[]>(0);
                maxUsuarios = planReader.GetInt32(1);
            }
            await planReader.CloseAsync();

            await using var tx = await conn.BeginTransactionAsync(ct);
            try
            {
                // 1. Crear schema
                await using var schemaCmd = conn.CreateCommand();
                schemaCmd.Transaction = tx;
                schemaCmd.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{dto.Slug}\"";
                await schemaCmd.ExecuteNonQueryAsync(ct);

                // 2. Ejecutar DDL base del tenant (tablas transversales)
                var tenantSql = GenerateTenantDDL(dto.Slug);
                await using var ddlCmd = conn.CreateCommand();
                ddlCmd.Transaction = tx;
                ddlCmd.CommandText = tenantSql;
                await ddlCmd.ExecuteNonQueryAsync(ct);

                // 3. Registrar en master.tenants
                await using var insertCmd = conn.CreateCommand();
                insertCmd.Transaction = tx;
                insertCmd.CommandText = @"
                    INSERT INTO master.tenants (slug, nombre_comercial, razon_social, rfc, plan, schema_name, estado,
                                               max_usuarios, contacto_email, contacto_nombre, fecha_vencimiento)
                    VALUES ($1, $2, $3, $4, $5, $1, 'activo', $6, $7, $8, $9)
                    RETURNING id";
                insertCmd.Parameters.AddWithValue(dto.Slug);
                insertCmd.Parameters.AddWithValue(dto.NombreComercial);
                insertCmd.Parameters.AddWithValue((object?)dto.RazonSocial ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue((object?)dto.Rfc ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue(dto.Plan ?? "basico");
                insertCmd.Parameters.AddWithValue(maxUsuarios);
                insertCmd.Parameters.AddWithValue((object?)dto.ContactoEmail ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue((object?)dto.ContactoNombre ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue(dto.FechaVencimiento.HasValue
                    ? (object)dto.FechaVencimiento.Value : DBNull.Value);
                var tenantId = (Guid)(await insertCmd.ExecuteScalarAsync(ct))!;

                // 4. Insertar feature flags según plan
                foreach (var feature in planFeatures)
                {
                    await using var ffCmd = conn.CreateCommand();
                    ffCmd.Transaction = tx;
                    ffCmd.CommandText = @"
                        INSERT INTO master.feature_flags (tenant_id, feature, habilitado)
                        VALUES ($1, $2, TRUE)
                        ON CONFLICT (tenant_id, feature) DO NOTHING";
                    ffCmd.Parameters.AddWithValue(tenantId);
                    ffCmd.Parameters.AddWithValue(feature);
                    await ffCmd.ExecuteNonQueryAsync(ct);
                }

                // 5. Sembrar configuración empresa + usuario admin (Rol=1)
                // Nota: Npgsql no soporta múltiples sentencias parametrizadas en un solo
                // CommandText (protocolo extendido), por lo que se ejecutan por separado.
                var adminEmail = dto.ContactoEmail ?? $"admin@{dto.Slug}.com";
                var adminPassword = "Admin123!";
                var adminHash = HashPasswordPbkdf2(adminPassword);

                await using (var setPathCmd = conn.CreateCommand())
                {
                    setPathCmd.Transaction = tx;
                    setPathCmd.CommandText = $"SET search_path = \"{dto.Slug}\", public";
                    await setPathCmd.ExecuteNonQueryAsync(ct);
                }

                await using (var cfgCmd = conn.CreateCommand())
                {
                    cfgCmd.Transaction = tx;
                    cfgCmd.CommandText = "INSERT INTO configuracion_empresa (nombre_comercial) VALUES ($1) ON CONFLICT DO NOTHING";
                    cfgCmd.Parameters.AddWithValue(dto.NombreComercial);
                    await cfgCmd.ExecuteNonQueryAsync(ct);
                }

                await using (var userCmd = conn.CreateCommand())
                {
                    userCmd.Transaction = tx;
                    userCmd.CommandText = @"
                        INSERT INTO usuarios (nombre, email, password_hash, rol, activo)
                        VALUES ($1, $2, $3, 1, TRUE)
                        ON CONFLICT (email) DO NOTHING";
                    userCmd.Parameters.AddWithValue(dto.ContactoNombre ?? $"Administrador {dto.NombreComercial}");
                    userCmd.Parameters.AddWithValue(adminEmail);
                    userCmd.Parameters.AddWithValue(adminHash);
                    await userCmd.ExecuteNonQueryAsync(ct);
                }

                await using (var resetPathCmd = conn.CreateCommand())
                {
                    resetPathCmd.Transaction = tx;
                    resetPathCmd.CommandText = "SET search_path = public";
                    await resetPathCmd.ExecuteNonQueryAsync(ct);
                }

                // 6. Registrar migración
                await using var migCmd = conn.CreateCommand();
                migCmd.Transaction = tx;
                migCmd.CommandText = @"
                    INSERT INTO master.schema_migrations (tenant_id, version)
                    VALUES ($1, '001_initial') ON CONFLICT DO NOTHING";
                migCmd.Parameters.AddWithValue(tenantId);
                await migCmd.ExecuteNonQueryAsync(ct);

                await tx.CommitAsync(ct);

                return Results.Created($"/api/v1/saas-admin/tenants/{dto.Slug}",
                    APIResponse<object>.Success(new
                    {
                        tenantId, slug = dto.Slug, schema = dto.Slug,
                        adminEmail, adminPassword,
                        message = $"Taller '{dto.Slug}' provisionado con usuario admin: {adminEmail} / {adminPassword}"
                    }));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return Results.Problem($"Error provisionando taller: {ex.Message}");
            }
        })
        .MapToApiVersion(v1)
        .Produces<APIResponse<object>>(StatusCodes.Status201Created);

        // ── TENANTS: ACTUALIZAR ─────────────────────────────────────────────────

        saas.MapPut("/tenants/{slug}", async (
            string slug, TenantUpdateDto dto, IConfiguration config, CancellationToken ct) =>
        {
            var connStr = DatabaseExtensions.BuildConnectionString(config);
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE master.tenants SET
                    nombre_comercial = $2, razon_social = $3, rfc = $4,
                    plan = $5, max_usuarios = $6,
                    contacto_email = $7, contacto_nombre = $8, fecha_vencimiento = $9,
                    updated_at = NOW()
                WHERE slug = $1";
            cmd.Parameters.AddWithValue(slug);
            cmd.Parameters.AddWithValue(dto.NombreComercial);
            cmd.Parameters.AddWithValue((object?)dto.RazonSocial ?? DBNull.Value);
            cmd.Parameters.AddWithValue((object?)dto.Rfc ?? DBNull.Value);
            cmd.Parameters.AddWithValue(dto.Plan);
            cmd.Parameters.AddWithValue(dto.MaxUsuarios);
            cmd.Parameters.AddWithValue((object?)dto.ContactoEmail ?? DBNull.Value);
            cmd.Parameters.AddWithValue((object?)dto.ContactoNombre ?? DBNull.Value);
            cmd.Parameters.AddWithValue(dto.FechaVencimiento.HasValue
                ? (object)dto.FechaVencimiento.Value : DBNull.Value);
            var rows = await cmd.ExecuteNonQueryAsync(ct);

            if (rows == 0)
                return Results.NotFound(APIResponse.Failure($"Taller '{slug}' no encontrado."));

            if (!string.IsNullOrEmpty(dto.Plan))
            {
                await using var planCmd = conn.CreateCommand();
                planCmd.CommandText = "SELECT features FROM master.planes WHERE slug = $1";
                planCmd.Parameters.AddWithValue(dto.Plan);
                var featuresObj = await planCmd.ExecuteScalarAsync(ct);
                if (featuresObj is string[] features)
                {
                    await using var tidCmd = conn.CreateCommand();
                    tidCmd.CommandText = "SELECT id FROM master.tenants WHERE slug = $1";
                    tidCmd.Parameters.AddWithValue(slug);
                    var tenantId = (Guid)(await tidCmd.ExecuteScalarAsync(ct))!;

                    await using var offCmd = conn.CreateCommand();
                    offCmd.CommandText = "UPDATE master.feature_flags SET habilitado = FALSE, updated_at = NOW() WHERE tenant_id = $1";
                    offCmd.Parameters.AddWithValue(tenantId);
                    await offCmd.ExecuteNonQueryAsync(ct);

                    foreach (var f in features)
                    {
                        await using var onCmd = conn.CreateCommand();
                        onCmd.CommandText = @"
                            INSERT INTO master.feature_flags (tenant_id, feature, habilitado)
                            VALUES ($1, $2, TRUE)
                            ON CONFLICT (tenant_id, feature) DO UPDATE SET habilitado = TRUE, updated_at = NOW()";
                        onCmd.Parameters.AddWithValue(tenantId);
                        onCmd.Parameters.AddWithValue(f);
                        await onCmd.ExecuteNonQueryAsync(ct);
                    }
                }
            }

            return Results.Ok(APIResponse.Success($"Taller '{slug}' actualizado."));
        })
        .MapToApiVersion(v1)
        .Produces<APIResponse>();

        // ── TENANTS: CAMBIAR ESTADO ─────────────────────────────────────────────

        saas.MapPatch("/tenants/{slug}/estado", async (
            string slug, TenantEstadoDto dto, IConfiguration config, CancellationToken ct) =>
        {
            if (dto.Estado is not ("activo" or "suspendido" or "cancelado"))
                return Results.BadRequest(APIResponse.Failure("Estado inválido. Valores: activo, suspendido, cancelado."));

            var connStr = DatabaseExtensions.BuildConnectionString(config);
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE master.tenants SET estado = $1, updated_at = NOW() WHERE slug = $2";
            cmd.Parameters.AddWithValue(dto.Estado);
            cmd.Parameters.AddWithValue(slug);
            var rows = await cmd.ExecuteNonQueryAsync(ct);

            if (rows == 0)
                return Results.NotFound(APIResponse.Failure($"Taller '{slug}' no encontrado."));

            return Results.Ok(APIResponse.Success($"Taller '{slug}' → {dto.Estado}."));
        })
        .MapToApiVersion(v1)
        .Produces<APIResponse>();

        // ── TENANTS: TOGGLE FEATURE ─────────────────────────────────────────────

        saas.MapPatch("/tenants/{slug}/features/{feature}", async (
            string slug, string feature, FeatureToggleDto dto,
            IConfiguration config, CancellationToken ct) =>
        {
            var connStr = DatabaseExtensions.BuildConnectionString(config);
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO master.feature_flags (tenant_id, feature, habilitado)
                SELECT id, $2, $3 FROM master.tenants WHERE slug = $1
                ON CONFLICT (tenant_id, feature)
                DO UPDATE SET habilitado = EXCLUDED.habilitado, updated_at = NOW()";
            cmd.Parameters.AddWithValue(slug);
            cmd.Parameters.AddWithValue(feature);
            cmd.Parameters.AddWithValue(dto.Habilitado);
            await cmd.ExecuteNonQueryAsync(ct);

            var estado = dto.Habilitado ? "habilitado" : "deshabilitado";
            return Results.Ok(APIResponse.Success($"Feature '{feature}' {estado} para '{slug}'."));
        })
        .MapToApiVersion(v1)
        .Produces<APIResponse>();

        // ── PAGOS: LISTA POR TENANT ──────────────────────────────────────────────

        saas.MapGet("/tenants/{slug}/pagos", async (
            string slug, IConfiguration config, CancellationToken ct) =>
        {
            var connStr = DatabaseExtensions.BuildConnectionString(config);
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT p.id, p.periodo, p.monto, p.moneda, p.estado,
                       p.metodo_pago, p.referencia, p.fecha_pago, p.created_at
                FROM master.pagos p
                INNER JOIN master.tenants t ON t.id = p.tenant_id
                WHERE t.slug = $1
                ORDER BY p.periodo DESC, p.created_at DESC";
            cmd.Parameters.AddWithValue(slug);

            var pagos = new List<PagoDto>();
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                pagos.Add(new PagoDto(
                    r.GetGuid(0), r.GetString(1), r.GetDecimal(2), r.GetString(3),
                    r.GetString(4),
                    r.IsDBNull(5) ? null : r.GetString(5),
                    r.IsDBNull(6) ? null : r.GetString(6),
                    r.IsDBNull(7) ? null : r.GetDateTime(7),
                    r.GetDateTime(8)
                ));
            }
            return Results.Ok(APIResponse<List<PagoDto>>.Success(pagos));
        })
        .MapToApiVersion(v1)
        .Produces<APIResponse<List<PagoDto>>>();

        // ── PAGOS: REGISTRAR ────────────────────────────────────────────────────

        saas.MapPost("/tenants/{slug}/pagos", async (
            string slug, PagoCreateDto dto, IConfiguration config, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Periodo) || dto.Monto <= 0)
                return Results.BadRequest(APIResponse.Failure("Periodo y monto son requeridos."));

            var connStr = DatabaseExtensions.BuildConnectionString(config);
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            await using var tidCmd = conn.CreateCommand();
            tidCmd.CommandText = "SELECT id FROM master.tenants WHERE slug = $1";
            tidCmd.Parameters.AddWithValue(slug);
            var tenantIdObj = await tidCmd.ExecuteScalarAsync(ct);
            if (tenantIdObj == null)
                return Results.NotFound(APIResponse.Failure($"Taller '{slug}' no encontrado."));
            var tenantId = (Guid)tenantIdObj;

            await using var tx = await conn.BeginTransactionAsync(ct);
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO master.pagos (tenant_id, periodo, monto, moneda, estado, metodo_pago, referencia, fecha_pago)
                    VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
                    RETURNING id";
                cmd.Parameters.AddWithValue(tenantId);
                cmd.Parameters.AddWithValue(dto.Periodo);
                cmd.Parameters.AddWithValue(dto.Monto);
                cmd.Parameters.AddWithValue(dto.Moneda ?? "MXN");
                cmd.Parameters.AddWithValue(dto.Estado ?? "pagado");
                cmd.Parameters.AddWithValue((object?)dto.MetodoPago ?? DBNull.Value);
                cmd.Parameters.AddWithValue((object?)dto.Referencia ?? DBNull.Value);
                cmd.Parameters.AddWithValue(dto.FechaPago.HasValue ? (object)dto.FechaPago.Value : DBNull.Value);
                var pagoId = (Guid)(await cmd.ExecuteScalarAsync(ct))!;

                if ((dto.Estado ?? "pagado") == "pagado" && dto.FechaVencimiento.HasValue)
                {
                    await using var activarCmd = conn.CreateCommand();
                    activarCmd.Transaction = tx;
                    activarCmd.CommandText = @"
                        UPDATE master.tenants
                        SET estado = 'activo', fecha_vencimiento = $2, updated_at = NOW()
                        WHERE id = $1 AND estado IN ('activo', 'suspendido')";
                    activarCmd.Parameters.AddWithValue(tenantId);
                    activarCmd.Parameters.AddWithValue(dto.FechaVencimiento.Value);
                    await activarCmd.ExecuteNonQueryAsync(ct);
                }

                await tx.CommitAsync(ct);

                return Results.Created($"/api/v1/saas-admin/tenants/{slug}/pagos",
                    APIResponse<object>.Success(new { pagoId, slug, periodo = dto.Periodo, monto = dto.Monto }));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return Results.Problem($"Error registrando pago: {ex.Message}");
            }
        })
        .MapToApiVersion(v1)
        .Produces<APIResponse<object>>(StatusCodes.Status201Created);

        // ── PAGOS: MARCAR COMO PAGADO ───────────────────────────────────────────

        saas.MapPatch("/pagos/{id:guid}/pagar", async (
            Guid id, PagoMarcarPagadoDto dto, IConfiguration config, CancellationToken ct) =>
        {
            var connStr = DatabaseExtensions.BuildConnectionString(config);
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            await using var tx = await conn.BeginTransactionAsync(ct);
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE master.pagos
                    SET estado = 'pagado', metodo_pago = $2, referencia = $3, fecha_pago = COALESCE($4, NOW())
                    WHERE id = $1 AND estado = 'pendiente'
                    RETURNING tenant_id";
                cmd.Parameters.AddWithValue(id);
                cmd.Parameters.AddWithValue((object?)dto.MetodoPago ?? DBNull.Value);
                cmd.Parameters.AddWithValue((object?)dto.Referencia ?? DBNull.Value);
                cmd.Parameters.AddWithValue(dto.FechaPago.HasValue ? (object)dto.FechaPago.Value : DBNull.Value);

                var tenantIdObj = await cmd.ExecuteScalarAsync(ct);
                if (tenantIdObj == null)
                    return Results.NotFound(APIResponse.Failure("Pago no encontrado o ya pagado."));

                if (dto.FechaVencimiento.HasValue)
                {
                    var tenantId = (Guid)tenantIdObj;
                    await using var activarCmd = conn.CreateCommand();
                    activarCmd.Transaction = tx;
                    activarCmd.CommandText = @"
                        UPDATE master.tenants
                        SET estado = 'activo', fecha_vencimiento = $2, updated_at = NOW()
                        WHERE id = $1 AND estado IN ('activo', 'suspendido')";
                    activarCmd.Parameters.AddWithValue(tenantId);
                    activarCmd.Parameters.AddWithValue(dto.FechaVencimiento.Value);
                    await activarCmd.ExecuteNonQueryAsync(ct);
                }

                await tx.CommitAsync(ct);
                return Results.Ok(APIResponse.Success("Pago registrado correctamente."));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return Results.Problem($"Error: {ex.Message}");
            }
        })
        .MapToApiVersion(v1)
        .Produces<APIResponse>();

        // ── PAGOS: RESUMEN GLOBAL ───────────────────────────────────────────────

        saas.MapGet("/pagos/resumen", async (IConfiguration config, CancellationToken ct) =>
        {
            var connStr = DatabaseExtensions.BuildConnectionString(config);
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    COUNT(1) FILTER (WHERE estado = 'pagado') AS total_pagados,
                    COUNT(1) FILTER (WHERE estado = 'pendiente') AS total_pendientes,
                    COUNT(1) FILTER (WHERE estado = 'vencido') AS total_vencidos,
                    COALESCE(SUM(monto) FILTER (WHERE estado = 'pagado'), 0) AS monto_cobrado,
                    COALESCE(SUM(monto) FILTER (WHERE estado = 'pendiente'), 0) AS monto_pendiente
                FROM master.pagos";

            await using var r = await cmd.ExecuteReaderAsync(ct);
            await r.ReadAsync(ct);

            return Results.Ok(APIResponse<object>.Success(new
            {
                TotalPagados = r.GetInt64(0),
                TotalPendientes = r.GetInt64(1),
                TotalVencidos = r.GetInt64(2),
                MontoCobrado = r.GetDecimal(3),
                MontoPendiente = r.GetDecimal(4)
            }));
        })
        .MapToApiVersion(v1)
        .Produces<APIResponse<object>>();

        return saas;
    }

    /// <summary>
    /// Genera el DDL base para crear las tablas transversales del tenant (taller).
    /// Lee la plantilla desde Resources/tenant_ddl_template.sql y reemplaza {{SCHEMA}}.
    /// </summary>
    private static string GenerateTenantDDL(string schema)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Resources", "tenant_ddl_template.sql");
        if (!File.Exists(templatePath))
        {
            var devPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "tenant_ddl_template.sql");
            if (File.Exists(devPath))
                templatePath = devPath;
            else
                throw new FileNotFoundException(
                    $"No se encontró la plantilla DDL en '{templatePath}' ni en '{devPath}'. " +
                    "Asegúrese de que Resources/tenant_ddl_template.sql esté marcado como CopyToOutputDirectory.");
        }
        var template = File.ReadAllText(templatePath, System.Text.Encoding.UTF8);
        return template.Replace("{{SCHEMA}}", schema);
    }

    private static string HashPasswordPbkdf2(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(32);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt, 100_000,
            HashAlgorithmName.SHA512, 32);
        return $"v1.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }
}

// ── DTOs ────────────────────────────────────────────────────────────────────

public record PlanDto(
    Guid Id, string Slug, string Nombre, string? Descripcion,
    decimal PrecioMensual, decimal PrecioAnual, string Moneda,
    int MaxUsuarios, string[] Features, bool Activo, short Orden
);

public record TenantListDto(
    Guid Id, string Slug, string NombreComercial, string Plan, string Estado,
    string SchemaName, int MaxUsuarios, DateTime? FechaVencimiento,
    string? ContactoEmail, string? ContactoNombre,
    string? RazonSocial, string? Rfc,
    DateTime CreatedAt, DateTime? UpdatedAt, long FeaturesActivos
);

public record TenantDetailDto(
    Guid Id, string Slug, string NombreComercial,
    string? RazonSocial, string? Rfc, string? LogoUrl,
    string Plan, string Estado, string SchemaName, string DbHost,
    int MaxUsuarios, DateTime? FechaVencimiento,
    string? ContactoEmail, string? ContactoNombre,
    DateTime CreatedAt, DateTime? UpdatedAt,
    List<FeatureFlagDto> Features, List<MigrationDto> Migraciones
);

public record FeatureFlagDto(string Feature, bool Habilitado);
public record MigrationDto(string Version, DateTime AplicadoAt);

public record TenantCreateDto(
    string Slug, string NombreComercial, string? RazonSocial, string? Rfc,
    string? Plan, string? ContactoEmail, string? ContactoNombre,
    DateTime? FechaVencimiento
);

public record TenantUpdateDto(
    string NombreComercial, string? RazonSocial, string? Rfc,
    string Plan, int MaxUsuarios,
    string? ContactoEmail = null, string? ContactoNombre = null,
    DateTime? FechaVencimiento = null
);

public record TenantEstadoDto(string Estado);
public record FeatureToggleDto(bool Habilitado);

public record PagoDto(
    Guid Id, string Periodo, decimal Monto, string Moneda, string Estado,
    string? MetodoPago, string? Referencia, DateTime? FechaPago, DateTime CreatedAt
);

public record PagoCreateDto(
    string Periodo, decimal Monto, string? Moneda = "MXN", string? Estado = "pagado",
    string? MetodoPago = null, string? Referencia = null,
    DateTime? FechaPago = null, DateTime? FechaVencimiento = null
);

public record PagoMarcarPagadoDto(
    string? MetodoPago = null, string? Referencia = null,
    DateTime? FechaPago = null, DateTime? FechaVencimiento = null
);
