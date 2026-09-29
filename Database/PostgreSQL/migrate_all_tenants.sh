#!/usr/bin/env bash
# ============================================================
# EnfoqueAutomotriz — Aplicar un script de migración a TODOS los tenants activos
# Uso: ./migrate_all_tenants.sh <archivo.sql> <version>
# Ejemplo: ./migrate_all_tenants.sh 003_add_ordenes_servicio.sql 003_add_ordenes_servicio
# El script SQL debe estar escrito para ejecutarse dentro del search_path del tenant.
# ============================================================
set -euo pipefail

SQL_FILE="${1:?Uso: migrate_all_tenants.sh <archivo.sql> <version>}"
VERSION="${2:?Falta la etiqueta de versión (ej. 003_add_ordenes_servicio)}"

: "${DB_HOST:=fa00219vmx}"
: "${DB_PORT:=5432}"
: "${DB_NAME:=EnfoqueAutomotriz}"
: "${DB_USER:=devteam_prod}"
: "${PGPASSWORD:?Defina la variable de entorno PGPASSWORD antes de ejecutar este script}"

export PGPASSWORD

echo "==> Obteniendo lista de tenants activos..."
TENANTS=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -A -c \
  "SELECT slug FROM master.tenants WHERE estado = 'activo' ORDER BY slug;")

for SLUG in $TENANTS; do
  echo "==> Aplicando '$VERSION' a tenant '$SLUG'..."
  psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -v ON_ERROR_STOP=1 <<SQL
SET search_path = "$SLUG", public;
\i $SQL_FILE
SET search_path = public;

INSERT INTO master.schema_migrations (tenant_id, version)
SELECT id, '$VERSION' FROM master.tenants WHERE slug = '$SLUG'
ON CONFLICT (tenant_id, version) DO NOTHING;
SQL
done

echo "==> Migración '$VERSION' aplicada a $(echo "$TENANTS" | wc -l) tenant(s)."
