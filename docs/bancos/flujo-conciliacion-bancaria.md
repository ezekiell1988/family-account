# Flujo Estándar de Conciliación Bancaria en un ERP

## Objetivo

Comparar el saldo contable registrado en el sistema (libro mayor de bancos) contra el saldo real reportado en el estado de cuenta bancario, identificando y resolviendo diferencias.

---

## Actores

| Actor | Rol |
|---|---|
| Tesorero / Contador | Ejecuta y valida la conciliación |
| ERP | Fuente de verdad de movimientos contables |
| Banco | Fuente de verdad del saldo real |

---

## Flujo Principal

```
┌─────────────────────────────────────────────────────────────┐
│ 1. OBTENER ESTADO DE CUENTA BANCARIO                        │
│    - Descargar archivo del banco (CSV, TXT, XLS, OFX, MT940)│
│    - Período: mes/semana a conciliar                        │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│ 2. IMPORTAR AL ERP                                          │
│    - Parsear formato del banco                              │
│    - Normalizar: fecha, descripción, monto, referencia      │
│    - Crear tabla de "movimientos bancarios pendientes"       │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│ 3. OBTENER MOVIMIENTOS CONTABLES                            │
│    - Consultar asientos del libro mayor para la cuenta bank │
│    - Mismo período del estado de cuenta                     │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│ 4. PROCESO DE MATCHING (Cotejo Automático)                  │
│    - Cruzar por: monto + fecha + referencia                 │
│    - Tolerancia de fechas configurable (ej. ±3 días)        │
│    - Resultado: movimientos emparejados / no emparejados     │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│ 5. REVISIÓN MANUAL DE DIFERENCIAS                           │
│    - En banco pero NO en contabilidad → registrar asiento   │
│    - En contabilidad pero NO en banco → tránsito pendiente  │
│    - Monto diferente → error contable o bancario            │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│ 6. AJUSTES CONTABLES                                        │
│    - Comisiones bancarias no registradas                    │
│    - Intereses acreditados                                  │
│    - Cheques anulados / devueltos                           │
│    - Depósitos en tránsito                                  │
│    - Cheques en circulación (emitidos pero no cobrados)     │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│ 7. VALIDACIÓN FINAL                                         │
│    Saldo Banco                                              │
│    + Depósitos en tránsito                                  │
│    - Cheques en circulación                                 │
│    = Saldo Contable Ajustado                                │
│    ┌─ Si coincide → CONCILIACIÓN APROBADA                   │
│    └─ Si no coincide → volver al paso 5                     │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│ 8. CIERRE Y ARCHIVO                                         │
│    - Marcar período como conciliado (lock)                  │
│    - Generar reporte de conciliación (PDF/Excel)            │
│    - Guardar evidencia (estado de cuenta + reporte)         │
└─────────────────────────────────────────────────────────────┘
```

---

## Estados de un Movimiento

| Estado | Descripción |
|---|---|
| `Pendiente` | Importado del banco, sin cruzar |
| `Emparejado` | Cruzado con asiento contable |
| `En tránsito` | En contabilidad pero aún no en banco |
| `No identificado` | En banco sin asiento contable correspondiente |
| `Revisión manual` | Con diferencia de monto o fecha fuera de tolerancia |
| `Conciliado` | Aprobado y cerrado |

---

## Tipos de Diferencias Comunes

| Diferencia | Causa Típica | Acción |
|---|---|---|
| Movimiento en banco, no en libros | Comisión, interés, débito automático | Registrar asiento |
| Movimiento en libros, no en banco | Cheque en tránsito, depósito no procesado | Esperar o investigar |
| Montos distintos | Error de digitación, redondeo | Corregir asiento |
| Fechas desfasadas | Clearing bancario tarde | Ajustar tolerancia de fechas |
| Referencia duplicada | Pago doble | Anular uno y notificar |

---

## Fórmulas de Cuadre

```
Saldo Banco (estado de cuenta)
  + Depósitos en tránsito (registrados en libros, no en banco)
  − Cheques en circulación (emitidos en libros, no cobrados en banco)
  ± Errores bancarios
= SALDO BANCARIO AJUSTADO

Saldo Contable (libro mayor)
  + Notas de crédito bancarias no registradas
  − Notas de débito bancarias no registradas (comisiones, etc.)
  ± Errores contables
= SALDO CONTABLE AJUSTADO

SALDO BANCARIO AJUSTADO = SALDO CONTABLE AJUSTADO ✓
```

---

## Archivos de Banco Soportados (contexto family-account)

| Banco | Formato | Notas |
|---|---|---|
| BAC | `.txt` | Columnas fijas o delimitadas por pipe |
| BCR | `.xls` | HTML embebido en XLS; columnas: Fecha contable, Fecha transacción, Hora, Documento, Descripción, Débitos, Créditos |
| BNCR | `.csv` | Separado por comas, encoding Latin-1 |
| Coopealianza | `.xls` / `.xlsx` | Excel con encabezados variables |
| Davivienda | `.xls` | Excel legacy (xlrd) |

---

## Consideraciones de Implementación

- **Idempotencia**: re-importar el mismo archivo no debe duplicar movimientos (usar hash o referencia única del banco).
- **Lock de período**: una vez conciliado, los asientos del período no deben modificarse.
- **Multimoneda**: manejar CRC y USD por separado; no mezclar en la misma conciliación.
- **Tolerancia de fechas**: configurable por banco (clearing puede demorar 1–3 días hábiles).
- **Trazabilidad**: cada movimiento bancario debe quedar vinculado al asiento contable que lo cubre.


