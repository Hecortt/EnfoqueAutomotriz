using Asp.Versioning;
using EnfoqueAutomotriz.Domain.Auth;
using EnfoqueAutomotriz.Domain.Auth.DTOs;
using EnfoqueAutomotriz.Domain.Shared;
using EnfoqueAutomotriz.Api.Extensions;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Utility;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

// ─── Password hashing: PBKDF2 / HMAC-SHA512 ──────────────────────────────────
// Formato almacenado: "v1.{saltBase64}.{hashBase64}"

namespace EnfoqueAutomotriz.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder api, ApiVersion v1)
    {
        var auth = api.MapGroup("/auth").WithTags("Autenticación");

        auth.MapPost("/login", async (
            [FromBody] LoginRequestDto dto,
            IUsuarioRepository repo,
            IAccesoLogRepository accesoLogRepo,
            IRecaptchaService recaptcha,
            IConfiguration config,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            if (userAgent.Length > 500) userAgent = userAgent[..500];

            if (recaptcha.IsEnabled)
            {
                var (captchaValid, _, captchaError) = await recaptcha.ValidateTokenAsync(dto.RecaptchaToken, ct);
                if (!captchaValid)
                {
                    await accesoLogRepo.RegistrarAccesoAsync(new AccesoLog
                    {
                        Email = dto.Email,
                        Accion = AccionAcceso.LoginFallido,
                        IP = ip,
                        UserAgent = userAgent,
                        Exitoso = false,
                        Detalles = $"reCAPTCHA fallido: {captchaError}"
                    }, ct);
                    return Results.Problem("Validación de seguridad fallida. Intente de nuevo.", statusCode: 403);
                }
            }

            var usuario = await repo.GetByEmailAsync(dto.Email, ct);

            // Fallback: buscar en master.superadmins (SuperAdmin global, Rol=0)
            if (usuario == null)
            {
                var superAdmin = await TryGetSuperAdminAsync(dto.Email, dto.Password, config, ct);
                if (superAdmin != null)
                {
                    var saToken = GenerateJwtToken(superAdmin, config);
                    await accesoLogRepo.RegistrarAccesoAsync(new AccesoLog
                    {
                        Email = dto.Email,
                        Accion = AccionAcceso.Login,
                        IP = ip,
                        UserAgent = userAgent,
                        Exitoso = true,
                        Detalles = "Login SuperAdmin global"
                    }, ct);
                    return Results.Ok(APIResponse<LoginResponseDto>.Success(new LoginResponseDto(
                        superAdmin.Id, superAdmin.Email, superAdmin.NombreCompleto,
                        superAdmin.Rol, superAdmin.FotoUrl, saToken,
                        DateTime.UtcNow.AddHours(8), null
                    )));
                }
            }

            if (usuario == null)
            {
                await accesoLogRepo.RegistrarAccesoAsync(new AccesoLog
                {
                    Email = dto.Email,
                    Accion = AccionAcceso.LoginFallido,
                    IP = ip,
                    UserAgent = userAgent,
                    Exitoso = false,
                    Detalles = "Usuario no encontrado"
                }, ct);
                return Results.Unauthorized();
            }

            if (usuario.Bloqueado)
            {
                var minutosRestantes = usuario.FechaBloqueo.HasValue
                    ? Math.Max(0, 30 - (int)(DateTime.UtcNow - usuario.FechaBloqueo.Value).TotalMinutes)
                    : 0;

                await accesoLogRepo.RegistrarAccesoAsync(new AccesoLog
                {
                    UsuarioId = usuario.Id,
                    Email = dto.Email,
                    Accion = AccionAcceso.LoginFallido,
                    IP = ip,
                    UserAgent = userAgent,
                    Exitoso = false,
                    Detalles = $"Usuario bloqueado. Minutos restantes: {minutosRestantes}"
                }, ct);

                var mensaje = minutosRestantes > 0
                    ? $"Usuario bloqueado por demasiados intentos fallidos. Intente en {minutosRestantes} minutos."
                    : "Usuario bloqueado. Contacte al administrador.";
                return Results.Problem(mensaje, statusCode: 403);
            }

            if (!usuario.Activo)
            {
                await accesoLogRepo.RegistrarAccesoAsync(new AccesoLog
                {
                    UsuarioId = usuario.Id,
                    Email = dto.Email,
                    Accion = AccionAcceso.LoginFallido,
                    IP = ip,
                    UserAgent = userAgent,
                    Exitoso = false,
                    Detalles = "Usuario inactivo"
                }, ct);
                return Results.Problem("Usuario inactivo.", statusCode: 403);
            }

            bool isLegacyHash = !usuario.PasswordHash.StartsWith("v1.");
            if (!VerifyPassword(dto.Password, usuario.PasswordHash))
            {
                await repo.IncrementFailedAttemptsAsync(usuario.Id, ct);

                var intentosRestantes = Math.Max(0, 5 - (usuario.IntentosFallidos + 1));

                await accesoLogRepo.RegistrarAccesoAsync(new AccesoLog
                {
                    UsuarioId = usuario.Id,
                    Email = dto.Email,
                    Accion = AccionAcceso.LoginFallido,
                    IP = ip,
                    UserAgent = userAgent,
                    Exitoso = false,
                    Detalles = $"Contraseña incorrecta. Intentos restantes: {intentosRestantes}"
                }, ct);

                if (intentosRestantes == 0)
                {
                    await accesoLogRepo.RegistrarAccesoAsync(new AccesoLog
                    {
                        UsuarioId = usuario.Id,
                        Email = dto.Email,
                        Accion = AccionAcceso.BloqueoUsuario,
                        IP = ip,
                        UserAgent = userAgent,
                        Exitoso = true,
                        Detalles = "Bloqueado por 5 intentos fallidos consecutivos"
                    }, ct);
                }

                return Results.Unauthorized();
            }

            if (isLegacyHash)
            {
                usuario.PasswordHash = HashPassword(dto.Password);
                await repo.UpdateAsync(usuario, ct);
            }

            await repo.UpdateLastAccessAsync(usuario.Id, ct);

            await accesoLogRepo.RegistrarAccesoAsync(new AccesoLog
            {
                UsuarioId = usuario.Id,
                Email = dto.Email,
                Accion = AccionAcceso.Login,
                IP = ip,
                UserAgent = userAgent,
                Exitoso = true,
                Detalles = "Login exitoso"
            }, ct);

            var token = GenerateJwtToken(usuario, config);
            var expiration = DateTime.UtcNow.AddHours(8);

            return Results.Ok(APIResponse<LoginResponseDto>.Success(new LoginResponseDto(
                usuario.Id, usuario.Email, usuario.NombreCompleto, usuario.Rol,
                usuario.FotoUrl, token, expiration, null
            )));
        })
        .MapToApiVersion(v1)
        .AllowAnonymous()
        .Produces<APIResponse<LoginResponseDto>>()
        .Produces(StatusCodes.Status401Unauthorized);

        auth.MapPost("/register", async (
            [FromBody] UsuarioCreateDto dto,
            IUsuarioRepository repo,
            Services.ITenantLimitService limits,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var (allowed, limitError) = await limits.CheckUsuarioLimitAsync(ct);
            if (!allowed)
                return Results.Json(APIResponse.Failure(limitError!), statusCode: 403);

            var existing = await repo.GetByEmailAsync(dto.Email, ct);
            if (existing != null)
                return Results.Conflict(APIResponse.Failure("El email ya está registrado.", HttpStatusCode.Conflict));

            var usuario = new Usuario
            {
                Email = dto.Email.ToLower().Trim(),
                NombreCompleto = dto.NombreCompleto.Trim(),
                PasswordHash = HashPassword(dto.Password),
                Rol = dto.Rol,
                CreatedBy = GetUserId(user)
            };

            await repo.CreateAsync(usuario, ct);

            return Results.Created($"/api/v1/auth/usuarios/{usuario.Id}", APIResponse<object>.Success(new { usuario.Id, usuario.Email }, HttpStatusCode.Created));
        })
        .MapToApiVersion(v1)
        .RequireAuthorization("AccesoTotal")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status409Conflict);

        auth.MapGet("/me", async (
            ClaimsPrincipal user,
            IUsuarioRepository repo,
            IMapper mapper,
            CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            if (!userId.HasValue)
                return Results.Unauthorized();

            var usuario = await repo.GetByIdAsync(userId.Value, ct);
            if (usuario == null)
                return Results.NotFound(APIResponse.Failure("No encontrado", HttpStatusCode.NotFound));

            return Results.Ok(APIResponse<UsuarioDto>.Success(mapper.Map<UsuarioDto>(usuario)));
        })
        .MapToApiVersion(v1)
        .RequireAuthorization()
        .Produces<APIResponse<UsuarioDto>>();

        auth.MapGet("/usuarios", async (
            IUsuarioRepository repo,
            IMapper mapper,
            CancellationToken ct) =>
        {
            var usuarios = await repo.GetAllAsync(ct);
            return Results.Ok(APIResponse<IEnumerable<UsuarioDto>>.Success(usuarios.Select(u => mapper.Map<UsuarioDto>(u))));
        })
        .MapToApiVersion(v1)
        .RequireAuthorization("AccesoTotal")
        .Produces<APIResponse<List<UsuarioDto>>>();

        auth.MapGet("/usuarios/{id:guid}", async (
            Guid id,
            IUsuarioRepository repo,
            IMapper mapper,
            CancellationToken ct) =>
        {
            var usuario = await repo.GetByIdAsync(id, ct);
            if (usuario == null)
                return Results.NotFound(APIResponse.Failure("Usuario no encontrado", HttpStatusCode.NotFound));
            return Results.Ok(APIResponse<UsuarioDto>.Success(mapper.Map<UsuarioDto>(usuario)));
        })
        .MapToApiVersion(v1)
        .RequireAuthorization("AccesoTotal")
        .Produces<APIResponse<UsuarioDto>>();

        auth.MapPut("/usuarios/{id:guid}", async (
            Guid id,
            [FromBody] UsuarioUpdateDto dto,
            IUsuarioRepository repo,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var usuario = await repo.GetByIdAsync(id, ct);
            if (usuario == null)
                return Results.NotFound(APIResponse.Failure("Usuario no encontrado", HttpStatusCode.NotFound));

            usuario.NombreCompleto = dto.NombreCompleto.Trim();
            usuario.Rol = dto.Rol;
            usuario.Activo = dto.Activo;
            usuario.UpdatedBy = GetUserId(user);

            await repo.UpdateAsync(usuario, ct);
            return Results.NoContent();
        })
        .MapToApiVersion(v1)
        .RequireAuthorization("AccesoTotal")
        .Produces(StatusCodes.Status204NoContent);

        auth.MapPatch("/usuarios/{id:guid}/desbloquear", async (
            Guid id,
            IUsuarioRepository repo,
            CancellationToken ct) =>
        {
            var usuario = await repo.GetByIdAsync(id, ct);
            if (usuario == null)
                return Results.NotFound(APIResponse.Failure("Usuario no encontrado", HttpStatusCode.NotFound));

            usuario.Bloqueado = false;
            usuario.IntentosFallidos = 0;
            await repo.UpdateAsync(usuario, ct);
            return Results.NoContent();
        })
        .MapToApiVersion(v1)
        .RequireAuthorization("SoloAdminTaller")
        .Produces(StatusCodes.Status204NoContent);

        auth.MapPatch("/usuarios/{id:guid}/reset-password", async (
            Guid id,
            [FromBody] ResetPasswordDto dto,
            IUsuarioRepository repo,
            CancellationToken ct) =>
        {
            var usuario = await repo.GetByIdAsync(id, ct);
            if (usuario == null)
                return Results.NotFound(APIResponse.Failure("Usuario no encontrado", HttpStatusCode.NotFound));

            usuario.PasswordHash = HashPassword(dto.NuevoPassword);
            await repo.UpdateAsync(usuario, ct);
            return Results.NoContent();
        })
        .MapToApiVersion(v1)
        .RequireAuthorization("SoloAdminTaller")
        .Produces(StatusCodes.Status204NoContent);

        auth.MapDelete("/usuarios/{id:guid}", async (
            Guid id,
            IUsuarioRepository repo,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var currentUserId = GetUserId(user);
            if (currentUserId == id)
                return Results.BadRequest(APIResponse.Failure("No puedes eliminar tu propia cuenta."));

            var usuario = await repo.GetByIdAsync(id, ct);
            if (usuario == null)
                return Results.NotFound(APIResponse.Failure("Usuario no encontrado", HttpStatusCode.NotFound));

            await repo.DeleteAsync(id, ct);
            return Results.NoContent();
        })
        .MapToApiVersion(v1)
        .RequireAuthorization("SoloAdminTaller")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        auth.MapGet("/hash-status", async (
            IUsuarioRepository repo,
            CancellationToken ct) =>
        {
            var (total, legacy) = await repo.GetHashMigrationStatusAsync(ct);
            return Results.Ok(APIResponse<object>.Success(new
            {
                TotalUsuarios = total,
                HashLegacySHA256 = legacy,
                HashPBKDF2 = total - legacy,
                MigracionCompleta = legacy == 0
            }));
        })
        .MapToApiVersion(v1)
        .RequireAuthorization("SoloAdminTaller");

        return auth;
    }

    private static string GenerateJwtToken(Usuario usuario, IConfiguration config)
    {
        var secret = config.GetValue<string>("ApiSettings:Secret")!;
        var issuer = config.GetValue<string>("ApiSettings:Issuer");
        var audience = config.GetValue<string>("ApiSettings:Audience");

        List<Claim> claimsList =
        [
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.NombreCompleto),
            new(ClaimTypes.Role, usuario.Rol.ToString()),
            new("rol", usuario.Rol.ToString())
        ];

        var claims = claimsList.ToArray();

        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private const int Pbkdf2Iterations = 100_000;
    private const int Pbkdf2HashLength = 32;
    private const int Pbkdf2SaltLength = 32;

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(Pbkdf2SaltLength);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA512,
            Pbkdf2HashLength);
        return $"v1.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        if (storedHash.StartsWith("v1."))
        {
            var parts = storedHash.Split('.');
            if (parts.Length != 3) return false;

            byte[] salt, expected;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expected = Convert.FromBase64String(parts[2]);
            }
            catch { return false; }

            var actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA512,
                Pbkdf2HashLength);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

#pragma warning disable CA5350
        using var sha256 = SHA256.Create();
        var legacyHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
#pragma warning restore CA5350
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(legacyHash),
            Encoding.UTF8.GetBytes(storedHash));
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    /// <summary>
    /// Busca en master.superadmins un usuario global (Rol=0) que pueda entrar a cualquier tenant (taller).
    /// </summary>
    private static async Task<Usuario?> TryGetSuperAdminAsync(
        string email, string password, IConfiguration config, CancellationToken ct)
    {
        try
        {
            var connStr = Extensions.DatabaseExtensions.BuildConnectionString(config);
            if (string.IsNullOrEmpty(connStr)) return null;

            await using var conn = new Npgsql.NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, nombre, email, password_hash, activo
                FROM master.superadmins
                WHERE email = $1 AND activo = TRUE
                LIMIT 1";
            cmd.Parameters.AddWithValue(email);

            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return null;

            var storedHash = r.GetString(3);
            if (!VerifyPassword(password, storedHash)) return null;

            return new Usuario
            {
                Id = r.GetGuid(0),
                NombreCompleto = r.GetString(1),
                Email = r.GetString(2),
                PasswordHash = storedHash,
                Rol = 0,
                Activo = true
            };
        }
        catch
        {
            return null;
        }
    }
}
