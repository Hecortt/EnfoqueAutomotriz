# EnfoqueAutomotriz — Guía de arquitectura para agentes

Plataforma **SaaS multi-tenant** para talleres automotrices (desde talleres
pequeños de una sola especialidad hasta cadenas multi-sucursal). Arquitectura
de infraestructura portada desde **Obra360** (mismo patrón, distinto dominio
de negocio).

## Stack

- .NET 9, ASP.NET Core Minimal API (`EnfoqueAutomotriz.Api`)
- EF Core 9 + Npgsql (PostgreSQL) + `EFCore.NamingConventions` (snake_case)
- Razor Pages + Tailwind/HTMX (`EnfoqueAutomotriz.UI`), consume la API por HTTP
- Docker multi-stage (API + UI en el mismo contenedor vía supervisord)

## Estructura de la solución

```
EnfoqueAutomotriz.slnx
src/
├── EnfoqueAutomotriz.Domain/    Entidades EF Core (POCOs) + DTOs + Shared (BaseEntity, APIResponse, PaginatedResult)
├── EnfoqueAutomotriz.Api/       Minimal API + EF Core + multitenancy + JWT + SaaS admin
├── EnfoqueAutomotriz.UI/        Razor Pages, cliente HTTP de la API
└── EnfoqueAutomotriz.Utility/   Helpers compartidos (SD.cs: roles, constantes)
Database/PostgreSQL/             Scripts SQL: schema master + plantilla de tenant + provisioning
```

## Multi-tenancy (schema-per-tenant)

- Una sola base de datos `EnfoqueAutomotriz`, un schema `master` (metadatos
  SaaS: tenants, planes, feature_flags, pagos, schema_migrations,
  superadmins) y un schema PostgreSQL independiente por cada taller (tenant).
- **Resolución de tenant** (`Middleware/TenantResolutionMiddleware.cs`): header
  `X-Tenant-Slug` → subdominio → path `/t/{slug}/` → `TenantSettings:DefaultSlug`.
- **Aislamiento de datos**: `Context/TenantSchemaInterceptor.cs` ejecuta
  `SET search_path = "{schema}", public` en cada conexión Npgsql abierta.
- **Feature gate por plan**: `Middleware/FeatureGateMiddleware.cs` bloquea
  rutas cuyo feature no esté habilitado en `master.feature_flags`.
- **Cuotas por plan**: `Services/TenantLimitService.cs` (ej. `max_usuarios`).
- **Provisioning**: `POST /api/v1/saas-admin/tenants` (rol SuperAdmin=0) crea
  el schema, ejecuta `Resources/tenant_ddl_template.sql`, registra el tenant,
  copia feature flags del plan y crea el usuario admin (rol=1).

## Roles (claim `ClaimTypes.Role`, valor numérico)

| Rol | Valor | Descripción |
|---|---|---|
| SuperAdmin | 0 | Administración SaaS central (todos los talleres), vive en `master.superadmins` |
| AdminTaller | 1 | Dueño/gerente del taller (tenant) |
| Mecanico | 2 | Técnico/mecánico |
| Recepcionista | 3 | Asesor de servicio / recepción |
| Enfermeria | 4 | Reservado (heredado del patrón compartido) — no usado en taller |
| Facturacion | 5 | Facturación / contador |
| SoporteIT | 6 | Soporte técnico interno |

Ver `EnfoqueAutomotriz.Utility.SD.Roles` y las policies en
`Extensions/AuthExtensions.cs`.

## Planes SaaS

`basico` (taller pequeño, 1 especialidad) · `multiespecialidad` (varias áreas:
mecánica, hojalatería y pintura, eléctrico, diagnóstico) · `enterprise`
(cadena multi-sucursal). Seed en `Database/PostgreSQL/01_master_planes.sql`.

## Fase actual: solo infraestructura

El schema de tenant (`Resources/tenant_ddl_template.sql` /
`Database/PostgreSQL/02_tenant_schema.sql`) contiene **únicamente** tablas
transversales: `usuarios`, `accesos_log`, `auditoria_general`,
`permiso_roles`, `notificaciones`, `catalogos` (catálogo genérico
multi-propósito), `configuracion_empresa`.

Las tablas de negocio del taller automotriz (órdenes de servicio, vehículos,
refacciones, citas, especialidades, inventario, facturación, etc.) **no**
están implementadas todavía. Se agregarán siguiendo el mismo patrón usado en
Obra360: `Domain/<Modulo>` → `Context/Configurations/<Modulo>` → `Repository`
→ `Endpoints` → registro en `Program.cs`, más el script SQL incremental
correspondiente y su `master.schema_migrations`.

## Conexión a base de datos

- Mismo servidor PostgreSQL que Obra360: `fa00219vmx:5432`, usuario
  `devteam_prod`, base de datos `EnfoqueAutomotriz` (independiente de la BD
  `Obra360`).
- La cadena de conexión se arma programáticamente vía
  `DatabaseExtensions.BuildConnectionString` (usa `NpgsqlConnectionStringBuilder`
  para soportar passwords con caracteres especiales), leyendo
  `DatabaseSettings:Host/Port/Database/Username/Password` de la configuración.
- `appsettings.Development.json` está gitignored; usar `.env` (basado en
  `.env.example`) para Docker/producción.

## Build & Run

```powershell
dotnet build EnfoqueAutomotriz.slnx
dotnet run --project src\EnfoqueAutomotriz.Api
dotnet run --project src\EnfoqueAutomotriz.UI
```
