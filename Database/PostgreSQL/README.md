# EnfoqueAutomotriz — Base de datos PostgreSQL

Arquitectura multi-tenant **schema-per-tenant**: una sola base de datos
`EnfoqueAutomotriz`, un schema `master` con los metadatos SaaS (tenants,
planes, feature flags, pagos) y un schema PostgreSQL independiente por cada
taller (tenant).

## Orden de ejecución (setup inicial)

1. `00_master_schema.sql` — crea el schema `master` y sus tablas (tenants,
   feature_flags, pagos, schema_migrations) + funciones helper.
2. `01_master_planes.sql` — crea `master.planes` (con seed de los 3 planes:
   `basico`, `multiespecialidad`, `enterprise`) y `master.superadmins`.
3. `02_tenant_schema.sql` — plantilla del schema de un tenant (taller).
   Se usa con `provision_tenant.sh`, o manualmente:
   ```bash
   psql -h <host> -U <user> -d EnfoqueAutomotriz -v schema=tallerdemo -f 02_tenant_schema.sql
   ```

## Provisionar un tenant nuevo

Vía script bash (requiere `psql` y la variable `PGPASSWORD`):

```bash
export PGPASSWORD='...'
./provision_tenant.sh tallerdemo "Taller Demo" basico admin@tallerdemo.com
```

Vía API (recomendado en producción): `POST /api/v1/saas-admin/tenants`
(requiere JWT con rol SuperAdmin=0). El endpoint crea el schema, ejecuta el
DDL embebido (`src/EnfoqueAutomotriz.Api/Resources/tenant_ddl_template.sql`),
registra el tenant en `master.tenants`, copia los feature flags del plan y
crea un usuario administrador (rol=1) con password temporal `Admin123!`.

## Migrar todos los tenants

```bash
export PGPASSWORD='...'
./migrate_all_tenants.sh 003_add_ordenes_servicio.sql 003_add_ordenes_servicio
```

## Notas

- La plantilla real usada en runtime por el endpoint de provisioning es
  `src/EnfoqueAutomotriz.Api/Resources/tenant_ddl_template.sql` (marcada
  `CopyToOutputDirectory` en el `.csproj`). Debe mantenerse sincronizada con
  `02_tenant_schema.sql` y con las `EntityTypeConfiguration` de EF Core.
- Fase 1 (infraestructura): el schema de tenant solo contiene tablas
  transversales (`usuarios`, `accesos_log`, `auditoria_general`,
  `permiso_roles`, `notificaciones`, `catalogos`, `configuracion_empresa`).
  Las tablas de negocio del taller (órdenes de servicio, vehículos,
  refacciones, citas, especialidades, etc.) se agregarán en migraciones
  incrementales posteriores conforme se diseñe el dominio automotriz.
- Cadena de conexión: ver `src/EnfoqueAutomotriz.Api/appsettings.Development.json`
  (`DatabaseSettings`), construida programáticamente en tiempo de ejecución
  vía `DatabaseExtensions.BuildConnectionString` para soportar passwords con
  caracteres especiales.
