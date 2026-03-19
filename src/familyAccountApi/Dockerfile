# ─── Stage 1: Build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restaurar dependencias primero (caching por capas)
COPY ["FamilyAccountApi.csproj", "."]
RUN dotnet restore

# Compilar
COPY . .
RUN dotnet build "FamilyAccountApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ─── Stage 2: Publish ────────────────────────────────────────────────────────
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FamilyAccountApi.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    -p:UseAppHost=false

# ─── Stage 3: Runtime ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Usuario no-root para mayor seguridad
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
WORKDIR /app

COPY --from=publish /app/publish .

# Puerto HTTP (Azure Container Apps expone 8080 por defecto)
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

USER appuser
EXPOSE 8080

ENTRYPOINT ["dotnet", "FamilyAccountApi.dll"]
