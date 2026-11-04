# ─────────────────────────────────────────────────────────────────
# STAGE 1 – build
# ─────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solo el csproj primero para aprovechar la caché de capas
COPY ["Proyecto.csproj", "."]
RUN dotnet restore "Proyecto.csproj"

# Copiar el resto del código y publicar
COPY . .
RUN dotnet publish "Proyecto.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# ─────────────────────────────────────────────────────────────────
# STAGE 2 – runtime
# ─────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Instalar curl para el health check
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app
USER appuser

# La app escucha en HTTP 8080 dentro del contenedor.
# HTTPS lo maneja el proxy/load balancer externo.
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "Proyecto.dll"]
