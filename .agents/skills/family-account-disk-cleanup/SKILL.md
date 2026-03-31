---
name: family-account-disk-cleanup
description: >
  Guía completa para diagnosticar y liberar espacio en el servidor de producción de
  family-account cuando el disco se llena. Cubre: conexión SSH, diagnóstico por capas
  (disco, /var, SQL Server, Docker, logs del SO), orden de limpiezas seguras y
  verificación final. Usar SIEMPRE que el disco supere el 85% de uso o el servidor
  deje de responder por falta de espacio.
---

# Disk Cleanup — family-account Server

## Acceso al servidor

Las credenciales de conexión están en `credentials/azure_vm_linux.txt` y la llave privada
en `credentials/key.pem`. **Ambos archivos están en `.gitignore` — nunca subirlos al repo.**

```bash
# Desde la carpeta credentials/
ssh -i key.pem -o StrictHostKeyChecking=no azureuser@<IP>
```

Para ejecutar un comando sin abrir sesión interactiva:
```bash
ssh -i key.pem -o StrictHostKeyChecking=no azureuser@<IP> "<comando>"
```

---

## Fase 1 — Diagnóstico (solo lectura)

Ejecutar en orden. Cada paso construye sobre el anterior.

### Paso 1 — Estado general del disco

```bash
df -h
```

**Qué mirar:**
- `/dev/root` — partición principal. Si está al **85%+ es urgente**, al **95%+ es crítico**.
- `/mnt` — disco temporal de Azure, no afecta al sistema.

### Paso 2 — Directorios más pesados

```bash
sudo du -sh /* 2>/dev/null | sort -rh | head -20
```

En este servidor los focos habituales son:

| Directorio | Qué contiene |
|------------|-------------|
| `/var/opt/mssql/` | SQL Server: datos, logs de error, traces |
| `/var/lib/docker/` | Imágenes, contenedores, build cache |
| `/var/log/` | journal, syslog, btmp |

### Paso 3 — SQL Server: archivos de datos y logs de error

```bash
sudo find /var/opt/mssql -maxdepth 3 -type f 2>/dev/null | sudo xargs du -sh | sort -rh | head -20
```

**Archivos seguros de eliminar:**

| Patrón | Descripción | Comando |
|--------|-------------|---------|
| `errorlog.1`, `.2`, … `.N` | Error logs rotados (el activo es `errorlog` sin número) | `sudo rm /var/opt/mssql/log/errorlog.[0-9]` |
| `system_health_*.xel` antiguos | Extended Events históricos (conservar los 3 más recientes) | `sudo rm <nombre-exacto>.xel` |
| `log_NNN.trc` anteriores al último | Trace files (conservar el de número mayor) | `sudo rm /var/opt/mssql/log/log_<N>.trc` |
| `SQLDump*.mdmp`, `SQLDump*.log`, `SQLDump*.txt` | Crash dumps | `sudo rm /var/opt/mssql/log/SQLDump*` |
| `HkEngineEventFile_*.xel` | In-memory engine event files de sesiones cerradas | `sudo rm /var/opt/mssql/log/HkEngineEventFile_*.xel` |

> ❌ **Nunca eliminar:** `errorlog` (activo), `*.mdf`, `*.ldf`, el `.trc` y `.xel` más recientes.

### Paso 4 — SQL Server: transaction logs inflados

Desde el host (sqlcmd en `/opt/mssql-tools18/bin/sqlcmd`):

```bash
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<ver credentials/db.txt>" -No \
  -Q "DBCC SQLPERF(LOGSPACE);"
```

Si alguna BD tiene el log con **>40% de espacio NO usado**, candidata a SHRINKFILE.

### Paso 5 — Tablas grandes dentro de la BD principal (`budgetdb`)

```bash
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<PASSWORD>" -No -d budgetdb -Q "
SELECT TOP 10
    t.name AS tabla,
    p.rows AS filas,
    SUM(a.total_pages) * 8 / 1024 AS total_mb
FROM sys.tables t
INNER JOIN sys.indexes i ON t.object_id = i.object_id
INNER JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
GROUP BY t.name, p.rows
ORDER BY total_mb DESC;"
```

> ⚠️ En este proyecto `tbLog` ha sido históricamente el mayor consumidor (tabla acumuladora de payloads de email).

### Paso 6 — Docker

```bash
docker system df
docker ps -a
```

**Candidatos a limpiar:**
- **Build cache** (`RECLAIMABLE` en la línea `Build Cache`) → siempre seguro si no hay build activo.
- **Imágenes dangling** → sin nombre ni contenedor.
- ❌ No eliminar imágenes con contenedores activos (`STATUS = Up`).

### Paso 7 — Logs del SO

```bash
sudo du -sh /var/log/* 2>/dev/null | sort -rh | head -15
journalctl --disk-usage
```

**Candidatos seguros:**

| Archivo | Acción |
|---------|--------|
| `/var/log/syslog.1`, `syslog.N.gz` | `sudo rm /var/log/syslog.[1-9]* /var/log/syslog.*.gz` |
| `/var/log/btmp.1` | `sudo rm /var/log/btmp.1` |
| journal | `sudo journalctl --vacuum-size=200M` |

---

## Fase 2 — Limpieza (orden recomendado por impacto/riesgo)

Verificar `df -h` después de cada grupo.

### L1 — Docker build cache (~2–4GB, riesgo: ninguno)

```bash
docker builder prune -af
```

Sin impacto en contenedores ni imágenes activas.

### L2 — SQL Server error logs rotados (~1–2GB, riesgo: ninguno)

```bash
sudo find /var/opt/mssql/log -maxdepth 1 -name 'errorlog.[0-9]*' -delete
```

### L3 — systemd journal (~400–800MB, riesgo: ninguno)

```bash
sudo journalctl --vacuum-size=200M
```

### L4 — SQL Server XEL / TRC / crash dumps (~400–500MB, riesgo: ninguno)

```bash
# Conservar solo el .trc más reciente (el de número más alto)
# Revisar cuál es antes de ejecutar: ls -lh /var/opt/mssql/log/*.trc
sudo rm /var/opt/mssql/log/log_<N-3>.trc
sudo rm /var/opt/mssql/log/log_<N-2>.trc
sudo rm /var/opt/mssql/log/log_<N-1>.trc

# XEL: conservar los 3 más recientes
# Revisar antes: ls -lht /var/opt/mssql/log/system_health_*.xel
sudo rm /var/opt/mssql/log/system_health_<antiguos>.xel

# Crash dumps
sudo rm /var/opt/mssql/log/SQLDump*.mdmp 2>/dev/null
sudo rm /var/opt/mssql/log/SQLDump*.log  2>/dev/null
sudo rm /var/opt/mssql/log/SQLDump*.txt  2>/dev/null
sudo rm /var/opt/mssql/log/HkEngineEventFile_*.xel 2>/dev/null
```

### L5 — Syslog y btmp rotados (~400–500MB, riesgo: ninguno)

```bash
sudo rm -f /var/log/syslog.1
sudo rm -f /var/log/syslog.[3-9].gz
sudo rm -f /var/log/btmp.1
```

### L6 — SHRINKFILE transaction logs SQL Server (~500MB–1GB, riesgo: bajo)

Ejecutar desde sqlcmd en el servidor:

```sql
-- Para cada BD con log inflado (>40% libre)
USE <nombre_bd>;
CHECKPOINT;
DBCC SHRINKFILE (<nombre_bd>_log, 1);
```

> El nombre lógico del archivo log se obtiene con:
> ```sql
> SELECT name, physical_name FROM sys.database_files WHERE type_desc = 'LOG';
> ```

### L7 — TRUNCATE tabla de logs de aplicación (riesgo: medio — confirmar con el equipo)

Solo si la tabla identificada en el Paso 5 es una tabla de logs/auditoría **no operativa**:

```sql
USE budgetdb;
TRUNCATE TABLE tbLog;   -- o el nombre de la tabla identificada
CHECKPOINT;
DBCC SHRINKFILE (budgetdb, 1);
```

> ✅ `TRUNCATE` es mínimamente logueado — no llena el transaction log a diferencia de `DELETE`.  
> ❌ No usar si la tabla tiene datos que la aplicación necesita para operar.

---

## Verificación final

```bash
df -h
sudo du -sh /var/opt/mssql/* 2>/dev/null | sort -rh | head -5
docker system df
```

**Estado saludable objetivo:** `/dev/root` por debajo del **75%**.

---

## Reglas de seguridad

| ❌ Nunca hacer | ✅ Siempre hacer |
|---------------|----------------|
| Eliminar `.mdf` o `.ldf` activos | Verificar `df -h` después de cada paso |
| Eliminar imágenes Docker con contenedores `Up` | Identificar el archivo activo antes de rm |
| Ejecutar `TRUNCATE` sin confirmar que la tabla no es operativa | Usar `find` con `-name` exacto, no wildcards en datos |
| `rm -rf` en directorios que no sean log/cache | Revisar `docker ps -a` antes de `docker image prune` |

---

## Historial conocido de este servidor

| Fecha | Evento | Solución aplicada |
|-------|--------|------------------|
| 31 mar 2026 | Disco al 100% (57MB libres) | L1–L6 + TRUNCATE tbLog → 40% (18GB libres) |

**Causa raíz identificada (mar 2026):** `tbLog` acumuló 193K registros de emails con payload JSON completo (~44KB/registro = 9.8GB). Ver `docs/plan-ssh-disk-cleanup.md` para el diagnóstico completo.
