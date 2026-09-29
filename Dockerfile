# ============================================================
# EnfoqueAutomotriz — Multi-stage Dockerfile
# Construye API y UI en un solo contenedor (2 procesos)
# ============================================================

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar archivos de proyecto para restore (cache de NuGet)
COPY EnfoqueAutomotriz.slnx ./
COPY src/EnfoqueAutomotriz.Domain/EnfoqueAutomotriz.Domain.csproj src/EnfoqueAutomotriz.Domain/
COPY src/EnfoqueAutomotriz.Utility/EnfoqueAutomotriz.Utility.csproj src/EnfoqueAutomotriz.Utility/
COPY src/EnfoqueAutomotriz.Api/EnfoqueAutomotriz.Api.csproj src/EnfoqueAutomotriz.Api/
COPY src/EnfoqueAutomotriz.UI/EnfoqueAutomotriz.UI.csproj src/EnfoqueAutomotriz.UI/

RUN dotnet restore EnfoqueAutomotriz.slnx

# Copiar código fuente
COPY src/ src/

# Publicar API
RUN dotnet publish src/EnfoqueAutomotriz.Api/EnfoqueAutomotriz.Api.csproj \
    -c Release -o /app/api --no-restore

# Publicar UI
RUN dotnet publish src/EnfoqueAutomotriz.UI/EnfoqueAutomotriz.UI.csproj \
    -c Release -o /app/ui --no-restore

# ── Runtime ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Instalar supervisord para manejar 2 procesos
RUN apt-get update && apt-get install -y --no-install-recommends \
    supervisor \
    && rm -rf /var/lib/apt/lists/*

# Copiar builds
COPY --from=build /app/api /app/api
COPY --from=build /app/ui /app/ui

# Copiar scripts de BD (para provisioning desde el contenedor)
COPY Database/PostgreSQL/ /app/db/

# Configuración de supervisord
COPY docker/supervisord.conf /etc/supervisor/conf.d/enfoqueautomotriz.conf

# Variables de entorno por defecto
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS_API=http://+:7101 \
    ASPNETCORE_URLS_UI=http://+:7102 \
    ApiSettings__BaseUrl=http://localhost:7101

EXPOSE 7101 7102

CMD ["supervisord", "-c", "/etc/supervisor/conf.d/enfoqueautomotriz.conf"]
