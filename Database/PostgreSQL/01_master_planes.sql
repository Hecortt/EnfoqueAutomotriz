-- ============================================================
-- EnfoqueAutomotriz — Catálogo de planes SaaS + usuario SuperAdmin
-- Ejecutar DESPUÉS de 00_master_schema.sql
-- ============================================================

-- ── Catálogo de planes ──────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS master.planes (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    slug            VARCHAR(50)   NOT NULL UNIQUE,
    nombre          VARCHAR(100)  NOT NULL,
    descripcion     TEXT,
    precio_mensual  NUMERIC(10,2) NOT NULL DEFAULT 0,
    precio_anual    NUMERIC(10,2) NOT NULL DEFAULT 0,
    moneda          CHAR(3)       NOT NULL DEFAULT 'MXN',
    max_usuarios    INT           NOT NULL DEFAULT 5,
    features        TEXT[]        NOT NULL DEFAULT '{}',
    activo          BOOLEAN       NOT NULL DEFAULT TRUE,
    orden           SMALLINT      NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ
);

-- Seed planes: taller pequeño (basico), taller multi-especialidad, cadena/enterprise
INSERT INTO master.planes (slug, nombre, descripcion, precio_mensual, precio_anual, max_usuarios, features, orden)
VALUES
    ('basico', 'Básico', 'Taller pequeño de una sola especialidad (ej. solo mecánica general)',
     990, 9900, 3,
     ARRAY['ordenes_servicio', 'clientes', 'vehiculos', 'catalogos'],
     1),
    ('multiespecialidad', 'Multiespecialidad', 'Taller mediano con varias áreas: mecánica, hojalatería y pintura, eléctrico, diagnóstico',
     2490, 24900, 10,
     ARRAY['ordenes_servicio', 'clientes', 'vehiculos', 'catalogos',
           'citas', 'inventario_refacciones', 'especialidades', 'reportes'],
     2),
    ('enterprise', 'Enterprise', 'Cadena de talleres multi-sucursal con acceso completo',
     4990, 49900, -1,
     ARRAY['ordenes_servicio', 'clientes', 'vehiculos', 'catalogos',
           'citas', 'inventario_refacciones', 'especialidades', 'reportes',
           'multi_sucursal', 'consola_sql', 'permisos_granulares', 'facturacion'],
     3)
ON CONFLICT (slug) DO UPDATE SET
    nombre = EXCLUDED.nombre,
    descripcion = EXCLUDED.descripcion,
    precio_mensual = EXCLUDED.precio_mensual,
    precio_anual = EXCLUDED.precio_anual,
    max_usuarios = EXCLUDED.max_usuarios,
    features = EXCLUDED.features,
    orden = EXCLUDED.orden,
    updated_at = NOW();

-- ── Usuario SuperAdmin (Rol=0) — vive en el schema master ──────────────────
-- No pertenece a ningún tenant (taller). Administra toda la plataforma SaaS.

CREATE TABLE IF NOT EXISTS master.superadmins (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre          VARCHAR(200) NOT NULL,
    email           VARCHAR(200) NOT NULL UNIQUE,
    password_hash   VARCHAR(500) NOT NULL,
    activo          BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ
);

-- Insertar SuperAdmin por defecto.
-- IMPORTANTE: password_hash es un placeholder — genere el hash real con
-- scripts/GenHash (formato PBKDF2 "v1.{salt}.{hash}") antes de usarlo en producción.
INSERT INTO master.superadmins (nombre, email, password_hash)
VALUES ('Super Admin', 'superadmin@enfoqueautomotriz.app', '$PLACEHOLDER_GENERATE_FROM_APP$')
ON CONFLICT (email) DO NOTHING;
