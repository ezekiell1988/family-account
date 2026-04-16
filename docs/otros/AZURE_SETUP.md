# Azure Setup — Family Account API

## Recursos a crear

| Recurso | Nombre sugerido | Notas |
|---|---|---|
| Resource Group | `rg-familyaccount-prod` | Contenedor de todos los recursos |
| Azure Container Registry | `acrfamilyaccount` | Almacena la imagen Docker |
| Azure Container Apps Environment | `cae-familyaccount` | Entorno de ejecución |
| Azure Container App | `ca-familyaccount-api` | La API en producción |
| User-Assigned Managed Identity | `id-familyaccount` | Identidad para acceder a Key Vault |
| Azure Key Vault | `kv-familyaccount` | Secretos de la aplicación |
| Azure Cache for Redis | `redis-familyaccount` | Blacklist JWT + refresh tokens |

> El SQL Server ya existe en `172.191.128.24,1433`.

---

## Paso 1 — Resource Group

```bash
az group create \
  --name rg-familyaccount-prod \
  --location eastus
```

---

## Paso 2 — Azure Container Registry

```bash
az acr create \
  --resource-group rg-familyaccount-prod \
  --name acrfamilyaccount \
  --sku Basic \
  --admin-enabled false
```

---

## Paso 3 — Key Vault

```bash
az keyvault create \
  --name kv-familyaccount \
  --resource-group rg-familyaccount-prod \
  --location eastus \
  --enable-rbac-authorization true
```

### 3.1 — Cargar los secretos

> Usar `--` como separador jerárquico. La app los lee como `Db:ConnectionString`, `Jwt:Secret`, etc.

```bash
KV="kv-familyaccount"

# Base de datos
az keyvault secret set --vault-name $KV \
  --name "Db--ConnectionString" \
  --value "Server=172.191.128.24,1433;Database=dbfa;User Id=sa;Password=Sqlfe6fce48!EFE7;TrustServerCertificate=True;MultipleActiveResultSets=true"

# Redis (obtener luego del Paso 5)
az keyvault secret set --vault-name $KV \
  --name "Redis--ConnectionString" \
  --value "<primaryKey de Redis>,ssl=True,abortConnect=False"

# JWT
az keyvault secret set --vault-name $KV --name "Jwt--Secret"    --value "<clave-min-32-chars-cambiar>"
az keyvault secret set --vault-name $KV --name "Jwt--Issuer"    --value "FamilyAccountApi"
az keyvault secret set --vault-name $KV --name "Jwt--Audience"  --value "FamilyAccountClients"
az keyvault secret set --vault-name $KV --name "Jwt--TokenExpirationMinutes"    --value "60"
az keyvault secret set --vault-name $KV --name "Jwt--RefreshTokenExpirationDays" --value "7"

# SMTP
az keyvault secret set --vault-name $KV --name "Smtp--Host"        --value "<smtp-host>"
az keyvault secret set --vault-name $KV --name "Smtp--Port"        --value "587"
az keyvault secret set --vault-name $KV --name "Smtp--Username"    --value "<smtp-user>"
az keyvault secret set --vault-name $KV --name "Smtp--Password"    --value "<smtp-password>"
az keyvault secret set --vault-name $KV --name "Smtp--FromEmail"   --value "<noreply@dominio.com>"
az keyvault secret set --vault-name $KV --name "Smtp--FromName"    --value "Family Account"
az keyvault secret set --vault-name $KV --name "Smtp--EnableSsl"   --value "true"
```

---

## Paso 4 — Managed Identity

```bash
az identity create \
  --name id-familyaccount \
  --resource-group rg-familyaccount-prod

# Guardar el clientId para usarlo en el Container App
IDENTITY_ID=$(az identity show \
  --name id-familyaccount \
  --resource-group rg-familyaccount-prod \
  --query id --output tsv)

IDENTITY_CLIENT_ID=$(az identity show \
  --name id-familyaccount \
  --resource-group rg-familyaccount-prod \
  --query clientId --output tsv)
```

### 4.1 — Asignar rol en Key Vault

```bash
KV_RESOURCE_ID=$(az keyvault show \
  --name kv-familyaccount \
  --resource-group rg-familyaccount-prod \
  --query id --output tsv)

az role assignment create \
  --assignee $IDENTITY_CLIENT_ID \
  --role "Key Vault Secrets User" \
  --scope $KV_RESOURCE_ID
```

### 4.2 — Dar acceso al ACR

```bash
ACR_RESOURCE_ID=$(az acr show \
  --name acrfamilyaccount \
  --resource-group rg-familyaccount-prod \
  --query id --output tsv)

az role assignment create \
  --assignee $IDENTITY_CLIENT_ID \
  --role "AcrPull" \
  --scope $ACR_RESOURCE_ID
```

---

## Paso 5 — Azure Cache for Redis

```bash
az redis create \
  --name redis-familyaccount \
  --resource-group rg-familyaccount-prod \
  --location eastus \
  --sku Basic \
  --vm-size C0

# Obtener primaryKey para el secreto de Key Vault
az redis list-keys \
  --name redis-familyaccount \
  --resource-group rg-familyaccount-prod \
  --query primaryKey --output tsv
```

> Con la primaryKey, volver al **Paso 3.1** y completar `Redis--ConnectionString`:
> ```
> redis-familyaccount.redis.cache.windows.net:6380,password=<primaryKey>,ssl=True,abortConnect=False
> ```

---

## Paso 6 — Publicar imagen Docker

```bash
# Login al ACR
az acr login --name acrfamilyaccount

# Build y push desde la carpeta del proyecto
cd src/familyAccountApi

docker build -t acrfamilyaccount.azurecr.io/familyaccount-api:latest .
docker push acrfamilyaccount.azurecr.io/familyaccount-api:latest
```

> O usando ACR Tasks (sin Docker local):
> ```bash
> az acr build \
>   --registry acrfamilyaccount \
>   --image familyaccount-api:latest \
>   --file src/familyAccountApi/Dockerfile \
>   src/familyAccountApi
> ```

---

## Paso 7 — Container Apps Environment

```bash
az containerapp env create \
  --name cae-familyaccount \
  --resource-group rg-familyaccount-prod \
  --location eastus
```

---

## Paso 8 — Container App

```bash
KV_URI=$(az keyvault show \
  --name kv-familyaccount \
  --resource-group rg-familyaccount-prod \
  --query properties.vaultUri --output tsv)

az containerapp create \
  --name ca-familyaccount-api \
  --resource-group rg-familyaccount-prod \
  --environment cae-familyaccount \
  --image acrfamilyaccount.azurecr.io/familyaccount-api:latest \
  --registry-identity $IDENTITY_ID \
  --user-assigned $IDENTITY_ID \
  --target-port 8080 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 3 \
  --cpu 0.5 \
  --memory 1.0Gi \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    AZURE_KEYVAULT_URI=$KV_URI \
    AZURE_CLIENT_ID=$IDENTITY_CLIENT_ID
```

> `AZURE_CLIENT_ID` es necesario cuando hay múltiples Managed Identities; le indica a `DefaultAzureCredential` cuál usar.

---

## Paso 9 — Verificar despliegue

```bash
# Ver URL pública
az containerapp show \
  --name ca-familyaccount-api \
  --resource-group rg-familyaccount-prod \
  --query properties.configuration.ingress.fqdn --output tsv

# Ver logs en tiempo real
az containerapp logs show \
  --name ca-familyaccount-api \
  --resource-group rg-familyaccount-prod \
  --follow
```

La API queda disponible en:
```
https://<fqdn>/api/v1/
https://<fqdn>/hangfire     ← admin / 12345
```

---

## Actualizar imagen (CI/CD)

```bash
# Build y push nueva versión
az acr build \
  --registry acrfamilyaccount \
  --image familyaccount-api:latest \
  --file src/familyAccountApi/Dockerfile \
  src/familyAccountApi

# Forzar nuevo despliegue
az containerapp update \
  --name ca-familyaccount-api \
  --resource-group rg-familyaccount-prod \
  --image acrfamilyaccount.azurecr.io/familyaccount-api:latest
```

---

## Resumen de variables de entorno del contenedor

| Variable | Valor |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_HTTP_PORTS` | `8080` |
| `AZURE_KEYVAULT_URI` | `https://kv-familyaccount.vault.azure.net/` |
| `AZURE_CLIENT_ID` | Client ID de la Managed Identity |

> Esas 4 son las **únicas** variables que el contenedor necesita.  
> Todos los demás secretos (DB, Redis, JWT, SMTP) viven en Key Vault.
