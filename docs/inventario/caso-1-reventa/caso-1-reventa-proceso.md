# Caso 1 — Reventa: Proceso completo

**Producto de ejemplo:** Coca-Cola 355ml (Producto 1, Tipo: Reventa, trackInventory = true)

> Ver configuraciones previas requeridas en [caso-1-reventa-configuraciones.md](./caso-1-reventa-configuraciones.md).

---

## Resumen del flujo

```
[COMPRA]          [VENTA]          [DEVOLUCIÓN]       [REINTEGRO]       [AJUSTE]
Proveedor →       Caja →           Inventario ↑       Caja →            Inventario ↓
Inventario ↑      Inventario ↓     COGS reversa       Ingresos ↓        Merma ↑
IVA Acreditable   IVA por Pagar    IVA por Pagar ↓
```

**Movimiento de stock:** 100 compradas − 10 vendidas + 3 devueltas − 2 regalía = **91 unidades**

---

## Paso 1 — Autenticación

El sistema usa autenticación en dos pasos:

1. `POST /auth/request-pin` con `emailUser` → el sistema envía un PIN por correo y lo guarda en `userPin`.
2. `POST /auth/login` con `emailUser` + `pin` → devuelve `accessToken` (JWT) y `refreshToken`.

Todos los pasos siguientes requieren `Authorization: Bearer <token>` en el header.

---

## Paso 2 — Pre-compra: vincular producto a cuenta contable

Antes de confirmar la factura de compra, el producto debe tener un registro `ProductAccount` que indique a qué cuenta contable se carga el costo:

| Campo | Valor |
|---|---|
| idProduct | 1 (Coca-Cola 355ml) |
| idAccount | 109 (1.1.07.01 Inventario de Mercadería) |
| percentageAccount | 100% |

> **¿Por qué?** El `ConfirmAsync` de `PurchaseInvoiceService` busca los `ProductAccount` del producto para construir las líneas DR del asiento. Si no existe ninguno, cae al `IdDefaultExpenseAccount` del tipo de factura (5.12.01 Gastos en Pareja).

> **Nota de ciclo de vida:** Una vez confirmada la compra, este `ProductAccount` se **elimina** para que la factura de venta use la cuenta `117 Ingresos` (del `IdAccountSalesRevenue` del tipo de venta), no la cuenta de inventario. Si no se elimina, la venta también intentaría acreditar la cuenta 109 como ingreso, lo cual es incorrecto.

---

## Paso 3 — Factura de compra (FC)

### 3a. Crear en borrador

`POST /purchase-invoices`

```json
{
  "idFiscalPeriod": 4,
  "idCurrency": 1,
  "idPurchaseInvoiceType": 1,
  "idContact": 1,
  "numberInvoice": "FAC-PROVEEDOR-C1-001",
  "dateInvoice": "2026-04-05",
  "subTotalAmount": 100000.00,
  "taxAmount": 13000.00,
  "totalAmount": 113000.00,
  "idWarehouse": 1,
  "exchangeRateValue": 1.0,
  "lines": [
    {
      "idProduct": 1,
      "idUnit": 1,
      "lotNumber": "LOT-COCA-C1-001",
      "expirationDate": "2027-12-31",
      "descriptionLine": "Coca-Cola 355ml × 100 un.",
      "quantity": 100,
      "unitPrice": 1000.00,
      "taxPercent": 13.00,
      "totalLineAmount": 113000.00
    }
  ]
}
```

Estado inicial: `Borrador`. No genera asiento ni lote todavía.

### 3b. Confirmar

`POST /purchase-invoices/{id}/confirm`

El sistema ejecuta en orden:

1. **Asiento contable FC-000001** (automático, OriginModule = "PurchaseInvoice"):

   | Cuenta | Nombre | DR | CR |
   |---|---|---|---|
   | 109 | Inventario de Mercadería | 100,000.00 | |
   | 124 | IVA Acreditable CRC (₡) | 13,000.00 | |
   | 106 | Caja CRC (₡) | | 113,000.00 |

   > El monto de la línea DR a inventario usa `quantity × unitPrice` (monto neto sin IVA). El IVA va a una cuenta separada como activo recuperable.

2. **Lote de inventario creado** (InventoryLot):

   | Campo | Valor |
   |---|---|
   | idProduct | 1 |
   | lotNumber | LOT-COCA-C1-001 |
   | idWarehouse | 1 |
   | quantityAvailable | 100 |
   | unitCost | 1,000.00 (unitPrice / conversionFactor = 1,000 / 1) |
   | expirationDate | 2027-12-31 |

   > `unitCost` usa el precio neto sin IVA porque el IVA ya fue separado a la cuenta 124. Esto asegura que el COGS refleje el costo real del bien.

**Estado final de la factura:** `Confirmado`

---

## Paso 4 — Post-compra: eliminar ProductAccount

`DELETE /product-accounts/{idProductAccount}`

Se elimina el vínculo `ProductAccount` para que la venta use la lógica de fallback (`IdAccountSalesRevenue = 117`) en lugar de intentar acreditar la cuenta 109.

---

## Paso 5 — Factura de venta (FV)

### 5a. Obtener el lote

`GET /inventory-lots/by-product/1.json`

Devuelve los lotes disponibles con `quantityAvailable`. Se selecciona el lote `LOT-COCA-C1-001` (idInventoryLot).

### 5b. Crear en borrador

`POST /sales-invoices`

```json
{
  "idFiscalPeriod": 4,
  "idCurrency": 1,
  "idSalesInvoiceType": 1,
  "idContact": 1,
  "dateInvoice": "2026-04-05",
  "subTotalAmount": 15000.00,
  "taxAmount": 1950.00,
  "totalAmount": 16950.00,
  "exchangeRateValue": 1.0,
  "lines": [
    {
      "isNonProductLine": false,
      "idProduct": 1,
      "idInventoryLot": 1,
      "descriptionLine": "Coca-Cola 355ml × 10 un.",
      "quantity": 10,
      "unitPrice": 1500.00,
      "taxPercent": 13.00,
      "totalLineAmount": 16950.00
    }
  ]
}
```

> El campo `idInventoryLot` es obligatorio para productos con inventario habilitado y sin receta activa. Indica de qué lote se descuentan las unidades.

### 5c. Confirmar

`POST /sales-invoices/{id}/confirm`

El sistema ejecuta en orden:

1. **Asiento de ingresos FV-20260405-001** (automático, OriginModule = "SalesInvoice"):

   | Cuenta | Nombre | DR | CR |
   |---|---|---|---|
   | 106 | Caja CRC (₡) | 16,950.00 | |
   | 117 | Ingresos por Ventas — Mercadería | | 15,000.00 |
   | 127 | IVA por Pagar CRC (₡) | | 1,950.00 |

   > El monto CR a ingresos usa `quantity × unitPrice` (neto sin IVA). El IVA cobrado al cliente va a un pasivo a declarar al gobierno al fin del período.

2. **Asiento de COGS COGS-FV-000001** (automático, OriginModule = "COGS"):

   | Cuenta | Nombre | DR | CR |
   |---|---|---|---|
   | 119 | Costo de Ventas — Mercadería | 10,000.00 | |
   | 109 | Inventario de Mercadería | | 10,000.00 |

   > El monto se calcula como: `quantityBase × unitCost = 10 × 1,000 = 10,000`.

3. **Descuento de inventario:** el lote `LOT-COCA-C1-001` pasa de 100 a **90 unidades**.

4. **NumberInvoice asignado:** `FV-20260405-001` (correlativo por fecha, solo se asigna al confirmar; en Borrador es `"BORRADOR"`).

**Estado final:** `Confirmado`

---

## Paso 6 — Devolución parcial (3 cajas)

`POST /sales-invoices/{id}/partial-return`

```json
{
  "dateReturn": "2026-04-05",
  "descriptionReturn": "El cliente devuelve 3 cajas dañadas en tránsito",
  "lines": [
    {
      "idInventoryLot": 1,
      "quantity": 3,
      "totalLineAmount": 5085.00,
      "descriptionLine": "Coca-Cola 355ml × 3 un. — devolución parcial"
    }
  ]
}
```

El sistema genera automáticamente el **asiento de reversa COGS DEV-COGS-FV-000001**:

| Cuenta | Nombre | DR | CR |
|---|---|---|---|
| 109 | Inventario de Mercadería | 3,000.00 | |
| 119 | Costo de Ventas — Mercadería | | 3,000.00 |

> `3 × unitCost (1,000) = 3,000`. El inventario del lote sube de 90 a **93 unidades**.

---

## Paso 7 — Reintegro bancario al cliente (manual)

La devolución del efectivo al cliente se registra como un asiento manual porque el sistema no tiene (aún) un módulo de notas de crédito automáticas de efectivo.

`POST /accounting-entries`

```json
{
  "idFiscalPeriod": 4,
  "idCurrency": 1,
  "numberEntry": "REINTEGRO-FV-001",
  "dateEntry": "2026-04-05",
  "descriptionEntry": "Reintegro al cliente — devolución 3 cajas",
  "statusEntry": "Publicado",
  "exchangeRateValue": 1.0,
  "lines": [
    { "idAccount": 117, "debitAmount": 4500.00, "creditAmount": 0,
      "descriptionLine": "Reversa ingreso — 3 × ₡1,500" },
    { "idAccount": 127, "debitAmount":  585.00, "creditAmount": 0,
      "descriptionLine": "Reversa IVA — 3 × ₡195" },
    { "idAccount": 106, "debitAmount": 0, "creditAmount": 5085.00,
      "descriptionLine": "Salida de caja — reintegro al cliente" }
  ]
}
```

| Cuenta | Nombre | DR | CR |
|---|---|---|---|
| 117 | Ingresos por Ventas — Mercadería | 4,500.00 | |
| 127 | IVA por Pagar CRC (₡) | 585.00 | |
| 106 | Caja CRC (₡) | | 5,085.00 |

> El IVA de la devolución reduce el IVA por Pagar, porque ese ingreso ya no se realizó.

---

## Paso 8 — Ajuste de inventario / Regalía (−2 cajas)

### 8a. Crear en borrador

`POST /inventory-adjustments`

```json
{
  "idFiscalPeriod": 4,
  "idInventoryAdjustmentType": 1,
  "idCurrency": 1,
  "exchangeRateValue": 1.0,
  "dateAdjustment": "2026-04-05",
  "descriptionAdjustment": "Regalía cliente VIP — Responsable: Administrador",
  "lines": [
    {
      "idInventoryLot": 1,
      "quantityDelta": -2,
      "descriptionLine": "Salida por regalía — Coca-Cola × 2 u."
    }
  ]
}
```

### 8b. Confirmar

`POST /inventory-adjustments/{id}/confirm`

El sistema genera el **asiento AJ-000001**:

| Cuenta | Nombre | DR | CR |
|---|---|---|---|
| 113 | Faltantes de Inventario (Merma) | 2,000.00 | |
| 109 | Inventario de Mercadería | | 2,000.00 |

> `2 × unitCost (1,000) = 2,000`. El inventario del lote baja de 93 a **91 unidades**.

---

## Verificación final de stock

`GET /inventory-lots/stock/1.json`

```
91
```

**Fórmula:** 100 (compra) − 10 (venta) + 3 (devolución) − 2 (regalía) = **91 ✓**

---

## T-accounts finales

| Cta | Nombre | DR | CR | Saldo |
|---|---|---|---|---|
| 106 | Caja CRC | 16,950 | 118,085 | **CR 101,135** (pagos superan cobros) |
| 109 | Inventario | 103,000 | 12,000 | **DR 91,000** (= 91 u × ₡1,000 ✓) |
| 113 | Merma | 2,000 | 0 | **DR 2,000** |
| 117 | Ingresos por Ventas | 4,500 | 15,000 | **CR 10,500** |
| 119 | Costo de Ventas | 10,000 | 3,000 | **DR 7,000** |
| 124 | IVA Acreditable | 13,000 | 0 | **DR 13,000** (crédito fiscal de compra) |
| 127 | IVA por Pagar | 585 | 1,950 | **CR 1,365** (IVA neto a declarar) |
| **Σ** | **Partida doble** | **149,035** | **149,035** | ✓ |

### Interpretación contable

| Concepto | Monto |
|---|---|
| Ingresos netos por ventas | ₡10,500 |
| Costo de ventas neto (COGS) | ₡7,000 |
| **Margen bruto** | **₡3,500 (33.3%)** |
| IVA a pagar al gobierno (neto) | ₡1,365 |
| IVA Acreditable (crédito fiscal de compra) | ₡13,000 |
| **Posición IVA neta** | **Crédito a favor ₡11,635** |

> El crédito de IVA a favor surge porque la compra fue de ₡100,000 (base) mientras que las ventas netas fueron de ₡10,500. En un período con mayor volumen de ventas el IVA por pagar superaría al acreditable.

---

## Scripts de testing

| Script | Propósito |
|---|---|
| [proceso.sh](./proceso.sh) | Test E2E — corre el flujo completo paso a paso (74 checks) |
| [consultas.sh](./consultas.sh) | Verificación — lee el `.txt` de resultado y valida todos los documentos y asientos |
| [cuentas.sh](./cuentas.sh) | Análisis contable — construye T-accounts, verifica saldos y genera reporte `.txt` |

```bash
# Correr desde la raíz del repositorio:
bash docs/inventario/caso-1-reventa/proceso.sh    # RESET_DB=true para BD limpia
bash docs/inventario/caso-1-reventa/consultas.sh  # requiere resultado_caso1_*.txt
bash docs/inventario/caso-1-reventa/cuentas.sh
```
