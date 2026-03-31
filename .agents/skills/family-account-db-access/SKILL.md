---
name: family-account-db-access
description: >
  Guía para acceder directamente a la base de datos SQL Server del proyecto family-account
  usando sqlcmd. Indica dónde se encuentran las credenciales sensibles (credentials/db.txt)
  y cómo están protegidas del repositorio. Usar SIEMPRE que se necesite ejecutar consultas
  SQL directas, diagnosticar la BD, limpiar datos, revisar espacio o el estado de tablas.
---

# Acceso Directo a la Base de Datos — family-account

## Credenciales

Las credenciales están en `credentials/db.txt`, **fuera del repositorio git** (la carpeta
`/credentials/` está en `.gitignore`). **Nunca subir ese archivo al repo.**

```
credentials/db.txt
──────────────────
HOST:     172.191.128.24
PORT:     1433
USER:     sa
PASSWORD: <ver credentials/db.txt>
```

Base de datos: `dbfa`

## Conectar con sqlcmd (PowerShell)

```powershell
sqlcmd -S "172.191.128.24,1433" -U sa -P "<PASSWORD>" -C -d dbfa -Q "<QUERY>"
```

| Flag | Significado |
|------|-------------|
| `-S` | Host y puerto (`host,puerto`) |
| `-U` | Usuario SQL |
| `-P` | Contraseña |
| `-C` | Confiar en el certificado del servidor (TrustServerCertificate) |
| `-d` | Base de datos por defecto |
| `-Q` | Ejecutar query y cerrar |
| `-i` | Ejecutar un archivo `.sql` |
| `-o` | Redirigir salida a un archivo |

## Consultas de Diagnóstico Frecuentes

### Espacio libre en disco del servidor
```powershell
sqlcmd -S "172.191.128.24,1433" -U sa -P "<PASSWORD>" -C -Q "EXEC xp_fixeddrives;"
```
> Devuelve MB libres por unidad. `0` en C: significa disco lleno.

### Tamaño del archivo de datos y log
```sql
SELECT
    name,
    physical_name,
    size * 8 / 1024 AS size_mb,
    FILEPROPERTY(name,'SpaceUsed') * 8 / 1024 AS used_mb,
    (size - FILEPROPERTY(name,'SpaceUsed')) * 8 / 1024 AS free_mb,
    growth,
    is_percent_growth
FROM sys.database_files;
```

### Tablas más grandes por espacio en disco
```sql
SELECT TOP 20
    t.name AS table_name,
    p.rows,
    SUM(a.total_pages) * 8 / 1024 AS total_mb,
    SUM(a.used_pages)  * 8 / 1024 AS used_mb
FROM sys.tables t
INNER JOIN sys.indexes i ON t.object_id = i.object_id
INNER JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
GROUP BY t.name, p.rows
ORDER BY total_mb DESC;
```

### Shrink del log (liberar espacio después de limpiar datos)
```sql
USE dbfa;
DBCC SHRINKFILE (dbfa_log, 1);
```

### Shrink del archivo de datos (sólo en dev, no en prod)
```sql
USE dbfa;
DBCC SHRINKDATABASE (dbfa, 10);
```

## Ejecutar desde PowerShell leyendo la contraseña del archivo

```powershell
# Lee las credenciales desde credentials/db.txt
$creds = Get-Content "credentials/db.txt" | ForEach-Object {
    $k, $v = $_ -split ':\s+', 2
    @{ $k = $v }
} | ForEach-Object { $_ }

$host = ($creds | Where-Object { $_.Keys -eq 'HOST' }).Values
$port = ($creds | Where-Object { $_.Keys -eq 'PORT' }).Values
$user = ($creds | Where-Object { $_.Keys -eq 'USER' }).Values
$pass = ($creds | Where-Object { $_.Keys -eq 'PASSWORD' }).Values

sqlcmd -S "$host,$port" -U $user -P $pass -C -d dbfa -Q "SELECT @@VERSION"
```

O más directamente con un script auxiliar:

```powershell
# Parsear credentials/db.txt como hashtable
$creds = @{}
Get-Content "credentials/db.txt" | ForEach-Object {
    $k, $v = $_ -split ':\s+', 2
    $creds[$k.Trim()] = $v.Trim()
}
$conn = "-S `"$($creds['HOST']),$($creds['PORT'])`" -U $($creds['USER']) -P $($creds['PASSWORD']) -C -d dbfa"
Invoke-Expression "sqlcmd $conn -Q `"SELECT @@VERSION`""
```

## Seguridad — Reglas de Oro

- `credentials/db.txt` **nunca entra al repo** → verificado en `.gitignore` con `/credentials/`
- No poner contraseñas en scripts que sí se commitean; siempre referenciar el archivo de credenciales
- En CI/CD, usar variables de entorno o Azure Key Vault en lugar del archivo plano
- El usuario `sa` tiene privilegios totales; para operaciones de solo lectura crear un usuario con permisos limitados
