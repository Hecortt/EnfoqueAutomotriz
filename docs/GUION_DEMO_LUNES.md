# Guion de Demo — EnfoqueAutomotriz
## Cliente: Cadena de talleres (Servicio Automotriz Galgo — datos de ejemplo)

**Duración estimada:** 12-15 minutos
**URL:** https://localhost:7102 (o la URL pública si ya está desplegado)
**Credenciales Admin Taller:** `admin@galgo.com` / `Admin123!`
**Credenciales Super Admin (para el punto 5, cambio de plan en vivo):** `superadmin@enfoqueautomotriz.app` / `SuperAdmin2026!`

---

## Antes de empezar (checklist de seguridad)

- [ ] Verificar que API (puerto 7101) y UI (puerto 7102) estén corriendo
- [ ] Hacer login una vez antes de la reunión para "calentar" la sesión (JIT, cache)
- [ ] Tener este documento abierto en una segunda pantalla/pestaña
- [ ] Tener un celular a la mano para escanear el QR del PDF en vivo
- [ ] **No entrar** a: Nivel C (nómina fiscal, consola SQL, bot con envío real) — solo se mencionan verbalmente
- [ ] Si algo falla, no intentar arreglarlo en vivo — seguir con el siguiente punto del guion

---

## 1. Apertura — Dos dashboards: General y por Taller (3 min)

**Ruta:** Login → `/Index` (Dashboard General)

**Qué mostrar:**
- KPIs consolidados: sucursales, empleados, órdenes activas, ingresos y **margen de utilidad real**
- Gráfica de tendencia (línea) de ingresos vs. margen de los últimos 6 meses
- Gráfica de dona con la distribución de órdenes por estatus
- Gráfica de barras comparando ingresos por sucursal + tabla detallada

**Después, ir a Sidebar → "Dashboard por Taller":**
- Selector de sucursal → cambiar entre Centro / Norte / Industrial
- KPIs específicos de esa sucursal (órdenes, ingresos, margen, empleados, clientes atendidos)
- Gráfica de tendencia y distribución de estatus de **esa sucursal en particular**
- Tabla de **técnicos con más órdenes atendidas** (ranking de productividad)
- Tabla de **refacciones más usadas** en ese taller específico

**Guion sugerido:**
> "Aquí tienen dos vistas: la general, para ver el negocio completo como dueños de la cadena, y esta otra — por taller — para que cada gerente de sucursal vea solo lo suyo: cuánto vendió su taller, qué tan ocupados están sus mecánicos, y hasta qué refacciones se usan más ahí, para anticipar compras."

---

## 2. Núcleo del negocio — Órdenes de Servicio (3 min)

**Ruta:** Sidebar → Órdenes de Servicio

**Qué mostrar:**
1. Lista de órdenes con folios, clientes, estatus con colores
2. Clic en "Nueva orden" → mostrar el formulario (sucursal, cliente, vehículo, kilometraje, nivel de gasolina, problema reportado)
3. Abrir el detalle de una orden **ya existente con cotización pendiente** (buscar una en estatus "EsperandoAprobacion")
4. Mostrar los conceptos (mano de obra + refacciones), cómo se agregan en vivo
5. Mostrar el botón **"Imprimir / PDF"** → abrir el PDF generado (con QR)

**Guion sugerido:**
> "Aquí vive el corazón del taller: desde que el cliente deja el auto hasta que se entrega, con línea de tiempo, fotos de evidencia y cotización. Y este PDF que se genera automáticamente — miren el código QR — es la puerta de entrada al siguiente punto, que es lo que más les va a gustar."

---

## 3. El momento "wow" — Portal del cliente sin login (3 min)

**Preparación:** Antes de la demo, copia el link `/Seguimiento/{token}` de una orden con cotización pendiente (visible en el detalle de la orden, sección amarilla "Esperando aprobación").

**Qué mostrar:**
1. Escanear el QR del PDF con tu celular (o abrir el link directamente)
2. Se abre una página simple, sin pedir usuario ni contraseña
3. Mostrar la línea de tiempo del servicio con fotos
4. Mostrar los botones grandes **"Aprobar"** / **"Rechazar"** de la cotización pendiente
5. Dar clic en "Aprobar" **en vivo**
6. Regresar a la pantalla del taller (Almacén) y mostrar que se generó automáticamente una **solicitud de preparación**

**Guion sugerido:**
> "Su cliente no necesita descargar una app ni crear una cuenta. Escanea el código, ve el avance con fotos que suben sus propios mecánicos, y aprueba la cotización desde su celular. Y miren esto — en el momento que aprueba, el sistema **automáticamente** le avisa a almacén qué refacciones preparar. Nadie tuvo que llamar por teléfono."

---

## 4. Almacén — Control de inventario (2 min)

**Ruta:** Sidebar → Refacciones

**Qué mostrar:**
- Los 3 contadores: críticas (rojo), por reordenar (ámbar), normal (verde)
- Tabla ordenada mostrando primero las críticas
- Mencionar (sin necesariamente hacer clic) que desde ahí se genera una orden de compra al proveedor

**Guion sugerido:**
> "Aquí ven en un vistazo qué se está agotando. Ya no es 'nos dimos cuenta que no había balatas cuando el cliente ya estaba esperando' — el sistema avisa antes."

---

## 5. La visión de crecimiento — Cambio de plan en vivo (3 min)

**Ruta:** Cerrar sesión de `admin@galgo.com` → iniciar sesión con `superadmin@enfoqueautomotriz.app` / `SuperAdmin2026!` → Sidebar → Panel SaaS

**Ya probado y funcionando end-to-end:**
1. En el Panel SaaS se ve la tabla de talleres (tenants) con su plan actual
2. Selecciona "Básico" en el dropdown de "Servicio Automotriz Galgo" y da clic en "Aplicar"
3. Abre una segunda pestaña (o cierra sesión y vuelve a entrar como `admin@galgo.com`) → el sidebar **ya no muestra** "Almacén", "Compras" ni "RH" — desaparecieron en tiempo real
4. Regresa al Panel SaaS y cambia el plan de vuelta a "Enterprise" → los módulos reaparecen

**Guion sugerido:**
> "Lo más importante de todo esto: si mañana quieren abrir un taller nuevo, chiquito, con un solo mecánico, no tienen que comprar otro sistema. Empiezan con el plan básico — vean, le quito el plan ahora mismo — y miren cómo desaparecen Almacén y RH del menú al instante. Conforme crece, lo regresamos a Enterprise y todo vuelve a aparecer. Todo en la misma plataforma, con los mismos datos, sin migrar nada."

**Importante:** vuelve a dejar el plan en "Enterprise" antes de seguir con el resto de la demo (o antes de terminar), para no dejar el tenant limitado.

---

## 6. Organización — RH y Organigrama (2 min)

**Ruta:** Sidebar → RH → Organigrama

**Qué mostrar:**
- El árbol completo: Director General → Gerentes de Sucursal → Jefes de Taller → Mecánicos/Especialistas
- Mencionar que cada empleado tiene su ficha con puesto, sucursal y salario

**Guion sugerido:**
> "Como cadena, necesitan ver su estructura completa. Esto ya está armado con datos de ejemplo de sus 3 sucursales — dirección, gerentes, jefes de taller, mecánicos senior y junior, especialistas."

**Nota honesta si preguntan por nómina:**
> "El cálculo de nómina con ISR e IMSS está en diseño para la siguiente fase — por ahora tienen el organigrama y la ficha de cada empleado con su salario."

---

## 7. Cierre — Clientes y fidelización (2 min)

**Ruta:** Sidebar → Clientes

**Qué mostrar:**
- Un cliente recurrente (badge "Cliente recurrente · N visitas") vs. uno nuevo ("Primera visita")
- Entrar al detalle de un cliente recurrente → ver su historial de vehículos

**Guion sugerido:**
> "El sistema ya sabe si es la primera vez que viene o si es cliente de años — esto abre la puerta a promociones dirigidas, que también ya están armadas."

(Mencionar Promociones brevemente: "10% en frenos", "15% clientes frecuentes")

---

## Preguntas frecuentes esperadas — respuestas honestas

| Pregunta | Respuesta sugerida |
|---|---|
| ¿Ya calculan nómina con impuestos? | "El motor de nómina fiscal (ISR/IMSS) está diseñado y es la siguiente fase de desarrollo — el organigrama y las fichas de empleados ya están listos." |
| ¿El bot de WhatsApp ya manda mensajes reales? | "El motor de plantillas y el historial ya están construidos; falta conectar la cuenta de WhatsApp Business — es una integración de días, no de meses, una vez que definan qué proveedor usar." |
| ¿Puedo facturar con esto? | "La facturación electrónica (CFDI) es la siguiente fase; hoy el sistema genera el desglose y el PDF del servicio, pero no timbra fiscalmente." |
| ¿Cuánto tardarían en tenerlo listo para producción? | (Ajustar según la conversación comercial — no comprometerse en el momento) |
| ¿Mis datos están seguros / separados de otros clientes? | "Sí — cada taller vive en su propio espacio de base de datos aislado, es la arquitectura multi-tenant que ya está construida y probada." |

---

## Plan B si algo falla en vivo

1. Si la UI no carga: recargar la página (Ctrl+Shift+R) una vez, si persiste, seguir con capturas de pantalla de respaldo
2. Si un formulario da error: no insistir, pasar al siguiente punto del guion
3. Si el QR no escanea bien con el celular del cliente: copiar el link manualmente y abrirlo en una pestaña

---

## Datos de referencia rápida (por si preguntan números)

- 3 sucursales (Centro, Norte, Industrial)
- 34 empleados en el organigrama
- 45 clientes (31 recurrentes)
- 136 órdenes de servicio históricas + activas
- 55 refacciones en inventario (11 críticas, 8 por reordenar)
- Ingresos del mes actual: ~$162,500 MXN · Margen: ~55%
