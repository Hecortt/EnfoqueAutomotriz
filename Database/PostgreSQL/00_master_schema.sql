-- ============================================================
-- EnfoqueAutomotriz — Schema MASTER (SaaS central administration)
-- Ejecutar UNA VEZ en la base de datos compartida EnfoqueAutomotriz
-- ============================================================

-- Crear base de datos (ejecutar como superusuario si no existe)
-- CREATE DATABASE "EnfoqueAutomotriz" ENCODING 'UTF8' LC_COLLATE 'es_MX.UTF-8' LC_CTYPE 'es_MX.UTF-8';

-- Habilitar extensiones necesarias
CREATE EXTENSION IF NOT EXISTS "pgcrypto";   -- gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS "unaccent";   -- búsquedas sin acentos

-- ── Schema master ────────────────────────────────────────────────────────────
CREATE SCHEMA IF NOT EXISTS master;

-- ── Tenants (talleres) ───────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS master.tenants (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    slug              VARCHAR(50)  NOT NULL UNIQUE,   -- 'taller_juan', 'automotriz_del_valle'
    nombre_comercial  VARCHAR(200) NOT NULL,
    razon_social      VARCHAR(300),
    rfc               VARCHAR(13),
    logo_url          VARCHAR(500),
    plan              VARCHAR(50)  NOT NULL DEFAULT 'basico',  -- basico | multiespecialidad | enterprise
    estado            VARCHAR(20)  NOT NULL DEFAULT 'activo',  -- activo | suspendido | cancelado
    schema_name       VARCHAR(63)  NOT NULL UNIQUE,   -- nombre del schema PostgreSQL
    db_host           VARCHAR(255) NOT NULL DEFAULT 'localhost',
    created_at        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at        TIMESTAMPTZ,
    fecha_vencimiento TIMESTAMPTZ,
    contacto_email    VARCHAR(200),
    contacto_nombre   VARCHAR(200),
    max_usuarios      INT          NOT NULL DEFAULT 5,
    CONSTRAINT chk_estado CHECK (estado IN ('activo', 'suspendido', 'cancelado')),
    CONSTRAINT chk_plan   CHECK (plan   IN ('basico', 'multiespecialidad', 'enterprise'))
);

CREATE INDEX IF NOT EXISTS ix_tenants_slug   ON master.tenants (slug);
CREATE INDEX IF NOT EXISTS ix_tenants_estado ON master.tenants (estado);

-- ── Feature flags por tenant ──────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS master.feature_flags (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID         NOT NULL REFERENCES master.tenants(id) ON DELETE CASCADE,
    feature     VARCHAR(100) NOT NULL,   -- 'citas', 'inventario_refacciones', 'multi_sucursal', etc.
    habilitado  BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ,
    UNIQUE (tenant_id, feature)
);

CREATE INDEX IF NOT EXISTS ix_feature_flags_tenant ON master.feature_flags (tenant_id);

-- ── Pagos / suscripciones ────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS master.pagos (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID          NOT NULL REFERENCES master.tenants(id) ON DELETE CASCADE,
    periodo         VARCHAR(7)    NOT NULL,  -- 'YYYY-MM'
    monto           NUMERIC(12,2) NOT NULL,
    moneda          CHAR(3)       NOT NULL DEFAULT 'MXN',
    estado          VARCHAR(20)   NOT NULL DEFAULT 'pendiente', -- pendiente | pagado | vencido
    metodo_pago     VARCHAR(50),
    referencia      VARCHAR(200),
    fecha_pago      TIMESTAMPTZ,
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_pago_estado CHECK (estado IN ('pendiente', 'pagado', 'vencido'))
);

CREATE INDEX IF NOT EXISTS ix_pagos_tenant  ON master.pagos (tenant_id);
CREATE INDEX IF NOT EXISTS ix_pagos_periodo ON master.pagos (periodo);

-- ── Registro de migraciones por schema ───────────────────────────────────────
CREATE TABLE IF NOT EXISTS master.schema_migrations (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID         NOT NULL REFERENCES master.tenants(id) ON DELETE CASCADE,
    version     VARCHAR(100) NOT NULL,   -- '001_initial', '002_add_ordenes_servicio', etc.
    aplicado_at TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    checksum    VARCHAR(64),
    UNIQUE (tenant_id, version)
);

-- ── Función helper: obtener tenant por slug ───────────────────────────────────
CREATE OR REPLACE FUNCTION master.get_tenant_by_slug(p_slug VARCHAR)
RETURNS master.tenants AS $$
    SELECT * FROM master.tenants WHERE slug = p_slug AND estado = 'activo' LIMIT 1;
$$ LANGUAGE sql STABLE;

-- ── Función helper: verificar feature flag ────────────────────────────────────
CREATE OR REPLACE FUNCTION master.has_feature(p_tenant_id UUID, p_feature VARCHAR)
RETURNS BOOLEAN AS $$
    SELECT COALESCE(
        (SELECT habilitado FROM master.feature_flags
         WHERE tenant_id = p_tenant_id AND feature = p_feature),
        FALSE
    );
$$ LANGUAGE sql STABLE;

-- ── Vista: tenants activos con sus features ───────────────────────────────────
CREATE OR REPLACE VIEW master.v_tenants_activos AS
SELECT
    t.id,
    t.slug,
    t.nombre_comercial,
    t.plan,
    t.estado,
    t.schema_name,
    t.max_usuarios,
    t.fecha_vencimiento,
    ARRAY_AGG(ff.feature) FILTER (WHERE ff.habilitado = TRUE) AS features_activos
FROM master.tenants t
LEFT JOIN master.feature_flags ff ON ff.tenant_id = t.id
WHERE t.estado = 'activo'
GROUP BY t.id;
