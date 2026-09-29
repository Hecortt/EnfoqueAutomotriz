-- ============================================================
-- EnfoqueAutomotriz — Plantilla DDL del schema TENANT (taller)
-- Fase 1: solo tablas transversales de infraestructura SaaS.
-- Los módulos de negocio (órdenes de servicio, vehículos, refacciones,
-- citas, especialidades, etc.) se agregarán en scripts posteriores
-- conforme se diseñe el dominio automotriz.
--
-- Se ejecuta reemplazando {{SCHEMA}} por el slug del tenant.
-- Sincronizado con las EntityTypeConfiguration de EnfoqueAutomotriz.Api.
-- ============================================================

CREATE SCHEMA IF NOT EXISTS "{{SCHEMA}}";
SET search_path = "{{SCHEMA}}", public;

CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "unaccent";

-- ============================================================
-- AUTH
-- ============================================================

CREATE TABLE IF NOT EXISTS usuarios (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre            VARCHAR(200) NOT NULL,
    email             VARCHAR(200) NOT NULL UNIQUE,
    password_hash     VARCHAR(500) NOT NULL,
    rol               SMALLINT     NOT NULL DEFAULT 2,
    activo            BOOLEAN      NOT NULL DEFAULT TRUE,
    bloqueado         BOOLEAN      NOT NULL DEFAULT FALSE,
    intentos_fallidos SMALLINT     NOT NULL DEFAULT 0,
    bloqueado_hasta   TIMESTAMPTZ,
    ultimo_acceso     TIMESTAMPTZ,
    foto_url          VARCHAR(500),
    is_deleted        BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at        TIMESTAMPTZ,
    created_by        UUID,
    updated_by        UUID
);

CREATE INDEX IF NOT EXISTS ix_usuarios_email ON usuarios (email) WHERE NOT is_deleted;

CREATE TABLE IF NOT EXISTS accesos_log (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id  UUID,
    email       VARCHAR(200),
    accion      VARCHAR(50)  NOT NULL,
    ip          VARCHAR(45),
    user_agent  VARCHAR(500),
    exitoso     BOOLEAN      NOT NULL DEFAULT TRUE,
    fecha       TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_accesos_log_usuario ON accesos_log (usuario_id);
CREATE INDEX IF NOT EXISTS ix_accesos_log_fecha   ON accesos_log (fecha);

CREATE TABLE IF NOT EXISTS auditoria_general (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id      UUID,
    usuario_email   VARCHAR(200),
    entidad         VARCHAR(100) NOT NULL,
    entidad_id      VARCHAR(100),
    accion          VARCHAR(50)  NOT NULL,
    valores_previos TEXT,
    valores_nuevos  TEXT,
    fecha           TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    ip              VARCHAR(45),
    modulo          VARCHAR(100),
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ,
    created_by      UUID,
    updated_by      UUID,
    is_deleted      BOOLEAN      NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS ix_auditoria_usuario ON auditoria_general (usuario_id);
CREATE INDEX IF NOT EXISTS ix_auditoria_entidad ON auditoria_general (entidad);
CREATE INDEX IF NOT EXISTS ix_auditoria_fecha   ON auditoria_general (fecha);

CREATE TABLE IF NOT EXISTS permiso_roles (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    rol             SMALLINT     NOT NULL,
    modulo          VARCHAR(100) NOT NULL,
    descripcion     VARCHAR(500),
    puede_ver       BOOLEAN      NOT NULL DEFAULT FALSE,
    puede_crear     BOOLEAN      NOT NULL DEFAULT FALSE,
    puede_editar    BOOLEAN      NOT NULL DEFAULT FALSE,
    puede_eliminar  BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    updated_at      TIMESTAMPTZ,
    created_by      UUID,
    updated_by      UUID,
    is_deleted      BOOLEAN      NOT NULL DEFAULT FALSE,
    UNIQUE (rol, modulo)
);

CREATE INDEX IF NOT EXISTS ix_permiso_roles_rol ON permiso_roles (rol);

CREATE TABLE IF NOT EXISTS notificaciones (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id  UUID         NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    tipo        SMALLINT     NOT NULL DEFAULT 0,
    titulo      VARCHAR(200) NOT NULL,
    mensaje     VARCHAR(500) NOT NULL,
    url         VARCHAR(300),
    leida       BOOLEAN      NOT NULL DEFAULT FALSE,
    fecha_leida TIMESTAMPTZ,
    is_deleted  BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    updated_at  TIMESTAMPTZ,
    created_by  UUID,
    updated_by  UUID
);

CREATE INDEX IF NOT EXISTS ix_notificaciones_usuario_leida ON notificaciones (usuario_id, leida);

-- ============================================================
-- CATÁLOGOS GENÉRICOS Y CONFIGURACIÓN
-- ============================================================

CREATE TABLE IF NOT EXISTS catalogos (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tipo        VARCHAR(100) NOT NULL,
    nombre      VARCHAR(150) NOT NULL,
    descripcion VARCHAR(500),
    color       VARCHAR(20),
    icono       VARCHAR(50),
    orden       INTEGER      NOT NULL DEFAULT 0,
    activo      BOOLEAN      NOT NULL DEFAULT TRUE,
    es_sistema  BOOLEAN      NOT NULL DEFAULT FALSE,
    is_deleted  BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS ix_catalogos_tipo_nombre ON catalogos (tipo, nombre);

CREATE TABLE IF NOT EXISTS configuracion_empresa (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre_comercial    VARCHAR(200) NOT NULL DEFAULT 'Mi Taller',
    razon_social        VARCHAR(300),
    rfc                 VARCHAR(13),
    logo_url            VARCHAR(500),
    direccion           VARCHAR(500),
    telefono            VARCHAR(20),
    email               VARCHAR(200),
    sitio_web           VARCHAR(200),
    banco               VARCHAR(100),
    clabe               VARCHAR(18),
    cuenta_bancaria     VARCHAR(30),
    titular_cuenta      VARCHAR(200),
    moneda              VARCHAR(10)  NOT NULL DEFAULT 'MXN',
    leyenda_factura     TEXT,
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ,
    created_by          UUID,
    updated_by          UUID
);

-- ============================================================
-- TALLER AUTOMOTRIZ: sucursales, clientes, vehículos, órdenes de servicio,
-- almacén/compras, RH, promociones y comunicación con el cliente.
-- DDL generado desde las EntityTypeConfiguration de EF Core (fuente de verdad).
-- ============================================================

CREATE TABLE IF NOT EXISTS sucursales (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    nombre character varying(150) NOT NULL,
    direccion character varying(300),
    telefono character varying(20),
    activa boolean NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    updated_at timestamp with time zone,
    created_by uuid,
    updated_by uuid,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_sucursales PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS clientes (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    nombre character varying(200) NOT NULL,
    telefono character varying(20),
    email character varying(200),
    rfc character varying(13),
    direccion character varying(300),
    notas text,
    activo boolean NOT NULL,
    numero_visitas integer NOT NULL DEFAULT 0,
    primera_visita timestamp with time zone,
    ultima_visita timestamp with time zone,
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    updated_at timestamp with time zone,
    created_by uuid,
    updated_by uuid,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_clientes PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_clientes_telefono ON clientes (telefono);

CREATE TABLE IF NOT EXISTS vehiculos (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    cliente_id uuid NOT NULL,
    marca character varying(80) NOT NULL,
    modelo character varying(80) NOT NULL,
    anio integer NOT NULL,
    placas character varying(20) NOT NULL,
    color character varying(40),
    vin character varying(30),
    kilometraje integer NOT NULL,
    activo boolean NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    updated_at timestamp with time zone,
    created_by uuid,
    updated_by uuid,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_vehiculos PRIMARY KEY (id),
    CONSTRAINT fk_vehiculos_clientes_cliente_id FOREIGN KEY (cliente_id) REFERENCES clientes (id) ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS ix_vehiculos_cliente_id ON vehiculos (cliente_id);
CREATE INDEX IF NOT EXISTS ix_vehiculos_placas ON vehiculos (placas);

CREATE TABLE IF NOT EXISTS departamentos (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    descripcion character varying(500),
    nombre character varying(150) NOT NULL,
    orden integer NOT NULL DEFAULT 0,
    activo boolean NOT NULL,
    es_sistema boolean NOT NULL DEFAULT FALSE,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    updated_at timestamp with time zone,
    CONSTRAINT pk_departamentos PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS puestos (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    descripcion character varying(500),
    departamento_id uuid NOT NULL,
    nivel integer NOT NULL,
    nombre character varying(150) NOT NULL,
    orden integer NOT NULL DEFAULT 0,
    activo boolean NOT NULL,
    es_sistema boolean NOT NULL DEFAULT FALSE,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    updated_at timestamp with time zone,
    CONSTRAINT pk_puestos PRIMARY KEY (id),
    CONSTRAINT fk_puestos_departamentos_departamento_id FOREIGN KEY (departamento_id) REFERENCES departamentos (id) ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS ix_puestos_departamento_id ON puestos (departamento_id);

CREATE TABLE IF NOT EXISTS empleados (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    codigo character varying(20) NOT NULL,
    nombres character varying(150) NOT NULL,
    apellido_paterno character varying(100) NOT NULL,
    apellido_materno character varying(100),
    telefono character varying(20),
    email character varying(200),
    fecha_ingreso timestamp with time zone NOT NULL,
    fecha_baja timestamp with time zone,
    salario_diario numeric(10,2) NOT NULL,
    sucursal_id uuid,
    puesto_id uuid NOT NULL,
    jefe_directo_id uuid,
    foto_url text,
    activo boolean NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    updated_at timestamp with time zone,
    created_by uuid,
    updated_by uuid,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_empleados PRIMARY KEY (id),
    CONSTRAINT fk_empleados_empleados_jefe_directo_id FOREIGN KEY (jefe_directo_id) REFERENCES empleados (id) ON DELETE RESTRICT,
    CONSTRAINT fk_empleados_puestos_puesto_id FOREIGN KEY (puesto_id) REFERENCES puestos (id) ON DELETE RESTRICT,
    CONSTRAINT fk_empleados_sucursales_sucursal_id FOREIGN KEY (sucursal_id) REFERENCES sucursales (id) ON DELETE SET NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_empleados_codigo ON empleados (codigo);
CREATE INDEX IF NOT EXISTS ix_empleados_jefe_directo_id ON empleados (jefe_directo_id);
CREATE INDEX IF NOT EXISTS ix_empleados_puesto_id ON empleados (puesto_id);
CREATE INDEX IF NOT EXISTS ix_empleados_sucursal_id ON empleados (sucursal_id);

CREATE TABLE IF NOT EXISTS capacitaciones (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    titulo character varying(200) NOT NULL,
    descripcion character varying(1000),
    categoria character varying(100),
    duracion_horas integer NOT NULL DEFAULT 0,
    activo boolean NOT NULL DEFAULT TRUE,
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    updated_at timestamp with time zone,
    created_by uuid,
    updated_by uuid,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_capacitaciones PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS empleados_capacitaciones (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    empleado_id uuid NOT NULL,
    capacitacion_id uuid NOT NULL,
    estado character varying(20) NOT NULL DEFAULT 'Pendiente',
    fecha_asignacion timestamp with time zone NOT NULL DEFAULT (NOW()),
    fecha_completado timestamp with time zone,
    calificacion_final numeric(5,2),
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    updated_at timestamp with time zone,
    created_by uuid,
    updated_by uuid,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_empleados_capacitaciones PRIMARY KEY (id),
    CONSTRAINT fk_empleados_capacitaciones_empleados_empleado_id FOREIGN KEY (empleado_id) REFERENCES empleados (id) ON DELETE CASCADE,
    CONSTRAINT fk_empleados_capacitaciones_capacitaciones_capacitacion_id FOREIGN KEY (capacitacion_id) REFERENCES capacitaciones (id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ix_empleados_capacitaciones_empleado_id ON empleados_capacitaciones (empleado_id);
CREATE INDEX IF NOT EXISTS ix_empleados_capacitaciones_capacitacion_id ON empleados_capacitaciones (capacitacion_id);
CREATE UNIQUE INDEX IF NOT EXISTS ix_empleados_capacitaciones_empleado_capacitacion ON empleados_capacitaciones (empleado_id, capacitacion_id);


CREATE TABLE IF NOT EXISTS proveedores (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    contacto character varying(150),
    telefono character varying(20),
    email character varying(200),
    direccion character varying(300),
    nombre character varying(200) NOT NULL,
    orden integer NOT NULL DEFAULT 0,
    activo boolean NOT NULL,
    es_sistema boolean NOT NULL DEFAULT FALSE,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    updated_at timestamp with time zone,
    CONSTRAINT pk_proveedores PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS refacciones (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    sku character varying(50) NOT NULL,
    categoria character varying(100),
    unidad character varying(10) NOT NULL,
    costo numeric(12,2) NOT NULL,
    precio_venta numeric(12,2) NOT NULL,
    stock_actual integer NOT NULL,
    stock_minimo integer NOT NULL,
    punto_reorden integer NOT NULL,
    proveedor_id uuid,
    nombre character varying(200) NOT NULL,
    orden integer NOT NULL DEFAULT 0,
    activo boolean NOT NULL,
    es_sistema boolean NOT NULL DEFAULT FALSE,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    updated_at timestamp with time zone,
    CONSTRAINT pk_refacciones PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_refacciones_sku ON refacciones (sku);

CREATE TABLE IF NOT EXISTS movimientos_inventario (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    refaccion_id uuid NOT NULL,
    tipo character varying(20) NOT NULL,
    cantidad integer NOT NULL,
    motivo character varying(300),
    orden_servicio_id uuid,
    orden_compra_id uuid,
    fecha timestamp with time zone NOT NULL DEFAULT (NOW()),
    CONSTRAINT pk_movimientos_inventario PRIMARY KEY (id),
    CONSTRAINT fk_movimientos_inventario_refacciones_refaccion_id FOREIGN KEY (refaccion_id) REFERENCES refacciones (id) ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS ix_movimientos_inventario_refaccion_id ON movimientos_inventario (refaccion_id);

CREATE TABLE IF NOT EXISTS solicitudes_almacen (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    orden_servicio_id uuid NOT NULL,
    sucursal_id uuid NOT NULL,
    estado character varying(20) NOT NULL,
    fecha_solicitud timestamp with time zone NOT NULL,
    fecha_listo timestamp with time zone,
    observaciones character varying(500),
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    updated_at timestamp with time zone,
    created_by uuid,
    updated_by uuid,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_solicitudes_almacen PRIMARY KEY (id),
    CONSTRAINT fk_solicitudes_almacen_ordenes_servicio_orden_servicio_id FOREIGN KEY (orden_servicio_id) REFERENCES ordenes_servicio (id) ON DELETE RESTRICT,
    CONSTRAINT fk_solicitudes_almacen_sucursales_sucursal_id FOREIGN KEY (sucursal_id) REFERENCES sucursales (id) ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS ix_solicitudes_almacen_orden_servicio_id ON solicitudes_almacen (orden_servicio_id);

CREATE TABLE IF NOT EXISTS solicitudes_almacen_detalle (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    solicitud_almacen_id uuid NOT NULL,
    refaccion_id uuid NOT NULL,
    cantidad integer NOT NULL,
    CONSTRAINT pk_solicitudes_almacen_detalle PRIMARY KEY (id),
    CONSTRAINT fk_solicitudes_almacen_detalle_refacciones_refaccion_id FOREIGN KEY (refaccion_id) REFERENCES refacciones (id) ON DELETE RESTRICT,
    CONSTRAINT fk_solicitudes_almacen_detalle_solicitudes_almacen_solicitud_a FOREIGN KEY (solicitud_almacen_id) REFERENCES solicitudes_almacen (id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ix_solicitudes_almacen_detalle_refaccion_id ON solicitudes_almacen_detalle (refaccion_id);
CREATE INDEX IF NOT EXISTS ix_solicitudes_almacen_detalle_solicitud_almacen_id ON solicitudes_almacen_detalle (solicitud_almacen_id);

CREATE TABLE IF NOT EXISTS ordenes_compra (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    folio character varying(20) NOT NULL,
    proveedor_id uuid NOT NULL,
    sucursal_id uuid NOT NULL,
    estado character varying(20) NOT NULL,
    fecha_orden timestamp with time zone NOT NULL,
    fecha_esperada timestamp with time zone,
    fecha_recibida timestamp with time zone,
    total numeric(12,2) NOT NULL,
    observaciones character varying(500),
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    updated_at timestamp with time zone,
    created_by uuid,
    updated_by uuid,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_ordenes_compra PRIMARY KEY (id),
    CONSTRAINT fk_ordenes_compra_proveedores_proveedor_id FOREIGN KEY (proveedor_id) REFERENCES proveedores (id) ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_ordenes_compra_folio ON ordenes_compra (folio);
CREATE INDEX IF NOT EXISTS ix_ordenes_compra_proveedor_id ON ordenes_compra (proveedor_id);

CREATE TABLE IF NOT EXISTS ordenes_compra_detalle (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    orden_compra_id uuid NOT NULL,
    refaccion_id uuid NOT NULL,
    cantidad_pedida integer NOT NULL,
    cantidad_recibida integer NOT NULL DEFAULT 0,
    costo_unitario numeric(12,2) NOT NULL,
    CONSTRAINT pk_ordenes_compra_detalle PRIMARY KEY (id),
    CONSTRAINT fk_ordenes_compra_detalle_ordenes_compra_orden_compra_id FOREIGN KEY (orden_compra_id) REFERENCES ordenes_compra (id) ON DELETE CASCADE,
    CONSTRAINT fk_ordenes_compra_detalle_refacciones_refaccion_id FOREIGN KEY (refaccion_id) REFERENCES refacciones (id) ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS ix_ordenes_compra_detalle_orden_compra_id ON ordenes_compra_detalle (orden_compra_id);
CREATE INDEX IF NOT EXISTS ix_ordenes_compra_detalle_refaccion_id ON ordenes_compra_detalle (refaccion_id);

CREATE TABLE IF NOT EXISTS ordenes_servicio (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    folio character varying(20) NOT NULL,
    sucursal_id uuid NOT NULL,
    cliente_id uuid NOT NULL,
    vehiculo_id uuid NOT NULL,
    empleado_asignado_id uuid,
    problema_reportado character varying(1000) NOT NULL,
    kilometraje_recepcion integer NOT NULL,
    nivel_gasolina character varying(10),
    observaciones text,
    estado character varying(30) NOT NULL,
    estado_cotizacion character varying(20) NOT NULL,
    fecha_aprobacion_cotizacion timestamp with time zone,
    comentario_rechazo text,
    subtotal numeric(12,2) NOT NULL,
    descuento numeric(12,2) NOT NULL,
    promocion_id uuid,
    iva numeric(12,2) NOT NULL,
    total numeric(12,2) NOT NULL,
    anticipo numeric(12,2) NOT NULL,
    saldo numeric(12,2) NOT NULL,
    costo_refacciones numeric(12,2) NOT NULL,
    token_seguimiento uuid NOT NULL DEFAULT (gen_random_uuid()),
    fecha_recepcion timestamp with time zone NOT NULL,
    fecha_entrega_programada timestamp with time zone,
    fecha_entrega timestamp with time zone,
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    updated_at timestamp with time zone,
    created_by uuid,
    updated_by uuid,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_ordenes_servicio PRIMARY KEY (id),
    CONSTRAINT fk_ordenes_servicio_clientes_cliente_id FOREIGN KEY (cliente_id) REFERENCES clientes (id) ON DELETE RESTRICT,
    CONSTRAINT fk_ordenes_servicio_sucursales_sucursal_id FOREIGN KEY (sucursal_id) REFERENCES sucursales (id) ON DELETE RESTRICT,
    CONSTRAINT fk_ordenes_servicio_vehiculos_vehiculo_id FOREIGN KEY (vehiculo_id) REFERENCES vehiculos (id) ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS ix_ordenes_servicio_cliente_id ON ordenes_servicio (cliente_id);
CREATE INDEX IF NOT EXISTS ix_ordenes_servicio_estado ON ordenes_servicio (estado);
CREATE UNIQUE INDEX IF NOT EXISTS ix_ordenes_servicio_folio ON ordenes_servicio (folio);
CREATE INDEX IF NOT EXISTS ix_ordenes_servicio_sucursal_id ON ordenes_servicio (sucursal_id);
CREATE UNIQUE INDEX IF NOT EXISTS ix_ordenes_servicio_token_seguimiento ON ordenes_servicio (token_seguimiento);
CREATE INDEX IF NOT EXISTS ix_ordenes_servicio_vehiculo_id ON ordenes_servicio (vehiculo_id);

CREATE TABLE IF NOT EXISTS ordenes_servicio_conceptos (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    orden_servicio_id uuid NOT NULL,
    tipo character varying(20) NOT NULL,
    refaccion_id uuid,
    descripcion character varying(300) NOT NULL,
    cantidad numeric(10,2) NOT NULL,
    precio_unitario numeric(12,2) NOT NULL,
    costo_unitario numeric(12,2) NOT NULL,
    importe numeric(12,2) NOT NULL,
    especialidad_id uuid,
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    updated_at timestamp with time zone,
    created_by uuid,
    updated_by uuid,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_ordenes_servicio_conceptos PRIMARY KEY (id),
    CONSTRAINT fk_ordenes_servicio_conceptos_ordenes_servicio_orden_servicio_ FOREIGN KEY (orden_servicio_id) REFERENCES ordenes_servicio (id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ix_ordenes_servicio_conceptos_orden_servicio_id ON ordenes_servicio_conceptos (orden_servicio_id);

CREATE TABLE IF NOT EXISTS ordenes_servicio_fotos (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    orden_servicio_id uuid NOT NULL,
    url character varying(500) NOT NULL,
    nota character varying(300),
    estado_al_subir character varying(30) NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    updated_at timestamp with time zone,
    created_by uuid,
    updated_by uuid,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_ordenes_servicio_fotos PRIMARY KEY (id),
    CONSTRAINT fk_ordenes_servicio_fotos_ordenes_servicio_orden_servicio_id FOREIGN KEY (orden_servicio_id) REFERENCES ordenes_servicio (id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ix_ordenes_servicio_fotos_orden_servicio_id ON ordenes_servicio_fotos (orden_servicio_id);

CREATE TABLE IF NOT EXISTS ordenes_servicio_historial (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    orden_servicio_id uuid NOT NULL,
    estado character varying(30) NOT NULL,
    comentario character varying(500),
    fecha timestamp with time zone NOT NULL DEFAULT (NOW()),
    usuario_id uuid,
    CONSTRAINT pk_ordenes_servicio_historial PRIMARY KEY (id),
    CONSTRAINT fk_ordenes_servicio_historial_ordenes_servicio_orden_servicio_ FOREIGN KEY (orden_servicio_id) REFERENCES ordenes_servicio (id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ix_ordenes_servicio_historial_orden_servicio_id ON ordenes_servicio_historial (orden_servicio_id);

CREATE TABLE IF NOT EXISTS encuestas_satisfaccion (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    orden_servicio_id uuid NOT NULL,
    calificacion integer NOT NULL,
    comentario character varying(500),
    fecha timestamp with time zone NOT NULL DEFAULT (NOW()),
    CONSTRAINT pk_encuestas_satisfaccion PRIMARY KEY (id),
    CONSTRAINT fk_encuestas_satisfaccion_ordenes_servicio_orden_servicio_id FOREIGN KEY (orden_servicio_id) REFERENCES ordenes_servicio (id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_encuestas_satisfaccion_orden_servicio_id ON encuestas_satisfaccion (orden_servicio_id);

CREATE TABLE IF NOT EXISTS ordenes_servicio_danos (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    orden_servicio_id uuid NOT NULL,
    x numeric(5,2) NOT NULL,
    y numeric(5,2) NOT NULL,
    tipo character varying(20) NOT NULL,
    descripcion character varying(200),
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    CONSTRAINT pk_ordenes_servicio_danos PRIMARY KEY (id),
    CONSTRAINT fk_ordenes_servicio_danos_ordenes_servicio_orden_servicio_id FOREIGN KEY (orden_servicio_id) REFERENCES ordenes_servicio (id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ix_ordenes_servicio_danos_orden_servicio_id ON ordenes_servicio_danos (orden_servicio_id);

CREATE TABLE IF NOT EXISTS ordenes_servicio_mecanicos (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    orden_servicio_id uuid NOT NULL,
    empleado_id uuid NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    CONSTRAINT pk_ordenes_servicio_mecanicos PRIMARY KEY (id),
    CONSTRAINT fk_ordenes_servicio_mecanicos_ordenes_servicio_orden_servicio_ FOREIGN KEY (orden_servicio_id) REFERENCES ordenes_servicio (id) ON DELETE CASCADE,
    CONSTRAINT fk_ordenes_servicio_mecanicos_empleados_empleado_id FOREIGN KEY (empleado_id) REFERENCES empleados (id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ix_ordenes_servicio_mecanicos_orden_servicio_id ON ordenes_servicio_mecanicos (orden_servicio_id);
CREATE INDEX IF NOT EXISTS ix_ordenes_servicio_mecanicos_empleado_id ON ordenes_servicio_mecanicos (empleado_id);
CREATE UNIQUE INDEX IF NOT EXISTS ix_ordenes_servicio_mecanicos_orden_empleado ON ordenes_servicio_mecanicos (orden_servicio_id, empleado_id);

CREATE TABLE IF NOT EXISTS promociones (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    descripcion character varying(500),
    tipo_descuento character varying(20) NOT NULL,
    valor numeric(10,2) NOT NULL,
    fecha_inicio timestamp with time zone NOT NULL,
    fecha_fin timestamp with time zone NOT NULL,
    aplica_a character varying(30) NOT NULL,
    nombre character varying(150) NOT NULL,
    orden integer NOT NULL DEFAULT 0,
    activo boolean NOT NULL,
    es_sistema boolean NOT NULL DEFAULT FALSE,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    updated_at timestamp with time zone,
    CONSTRAINT pk_promociones PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS plantillas_mensaje (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    asunto character varying(200),
    cuerpo character varying(1000) NOT NULL,
    canal character varying(20) NOT NULL,
    nombre character varying(150) NOT NULL,
    orden integer NOT NULL DEFAULT 0,
    activo boolean NOT NULL,
    es_sistema boolean NOT NULL DEFAULT FALSE,
    is_deleted boolean NOT NULL DEFAULT FALSE,
    created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
    updated_at timestamp with time zone,
    CONSTRAINT pk_plantillas_mensaje PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS mensajes_enviados (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    cliente_id uuid NOT NULL,
    orden_servicio_id uuid,
    plantilla_mensaje_id uuid,
    canal character varying(20) NOT NULL,
    destinatario character varying(200) NOT NULL,
    contenido character varying(1000) NOT NULL,
    estado character varying(20) NOT NULL,
    fecha timestamp with time zone NOT NULL DEFAULT (NOW()),
    CONSTRAINT pk_mensajes_enviados PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_mensajes_enviados_cliente_id ON mensajes_enviados (cliente_id);

SET search_path = public;
