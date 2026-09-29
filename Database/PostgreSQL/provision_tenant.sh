#!/usr/bin/env bash
# ============================================================
# EnfoqueAutomotriz — Provisionar un nuevo tenant (taller)
# Uso: ./provision_tenant.sh <slug> <nombre_comercial> <plan> [contacto_email]
# Ejemplo: ./provision_tenant.sh tallerdemo "Taller Demo" basico admin@tallerdemo.com
# ============================================================
set -euo pipefail

SLUG="${1:?Uso: provision_tenant.sh <slug> <nombre_comercial> <plan> [contacto_email]}"
NOMBRE="${2:?Falta nombre_comercial}"
PLAN="${3:-basico}"
CONTACTO_EMAIL="${4:-admin@${SLUG}.com}"

: "${DB_HOST:=fa00219vmx}"
: "${DB_PORT:=5432}"
: "${DB_NAME:=EnfoqueAutomotriz}"
: "${DB_USER:=devteam_prod}"
: "${PGPASSWORD:?Defina la variable de entorno PGPASSWORD antes de ejecutar este script}"

export PGPASSWORD

echo "==> Provisionando tenant '$SLUG' ($NOMBRE, plan=$PLAN)..."

# Límites según plan
case "$PLAN" in
  basico)             MAX_USUARIOS=3 ;;
  multiespecialidad)  MAX_USUARIOS=10 ;;
  enterprise)         MAX_USUARIOS=-1 ;;
  *)                  MAX_USUARIOS=5 ;;
esac

psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -v ON_ERROR_STOP=1 <<SQL
-- 1. Crear schema del tenant y tablas base
\set schema $SLUG
\i 02_tenant_schema.sql

-- 2. Registrar en master.tenants
INSERT INTO master.tenants (slug, nombre_comercial, plan, schema_name, estado, max_usuarios, contacto_email)
VALUES ('$SLUG', '$NOMBRE', '$PLAN', '$SLUG', 'activo', $MAX_USUARIOS, '$CONTACTO_EMAIL')
ON CONFLICT (slug) DO NOTHING;

-- 3. Copiar features del plan
INSERT INTO master.feature_flags (tenant_id, feature, habilitado)
SELECT t.id, unnest(p.features), TRUE
FROM master.tenants t
JOIN master.planes p ON p.slug = t.plan
WHERE t.slug = '$SLUG'
ON CONFLICT (tenant_id, feature) DO NOTHING;

-- 4. Registrar migración inicial
INSERT INTO master.schema_migrations (tenant_id, version)
SELECT id, '001_initial' FROM master.tenants WHERE slug = '$SLUG'
ON CONFLICT (tenant_id, version) DO NOTHING;
SQL

echo "==> Tenant '$SLUG' provisionado correctamente."
echo "    Recuerda crear el usuario administrador (rol=1) vía POST /api/v1/saas-admin/tenants"
echo "    o insertándolo manualmente en el schema \"$SLUG\".usuarios"
