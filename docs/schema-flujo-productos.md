# Schema de Flujo — Productos, Compras, Producción y Venta

> Basado en: `analisis-modelo-productos.md` v3 · 3 de abril de 2026

---

## Tablas del modelo

### Catálogos base

```
unitOfMeasure
─────────────────────────────────────────
idUnit          INT PK AI
codeUnit        VARCHAR(10) UNIQUE NOT NULL   -- ML, GR, KG, BOT160, LATA400…
nameUnit        NVARCHAR(80) NOT NULL
typeUnit        VARCHAR(20) NOT NULL          -- Volumen | Masa | Unidad | Longitud


productType
─────────────────────────────────────────
idProductType   INT PK AI
nameProductType NVARCHAR(60) UNIQUE NOT NULL  -- Materia Prima | Prod. en Proceso
                                              -- Prod. Terminado | Reventa
```

### Catálogo de productos

```
product
─────────────────────────────────────────
idProduct       INT PK AI
codeProduct     VARCHAR(50) UNIQUE NOT NULL
nameProduct     NVARCHAR(200) NOT NULL
idProductType   INT FK → productType         NOT NULL
idUnit          INT FK → unitOfMeasure       NOT NULL   -- unidad base (inventario)
idProductParent INT FK → product             NULL       -- agrupa variantes (max 1 nivel)
averageCost     DECIMAL(18,6) NOT NULL DEFAULT 0        -- costo promedio en unidad base
                                                         -- se recalcula al confirmar compras
                                                         -- y ajustes con stock positivo


productUnit
─────────────────────────────────────────
idProductUnit       INT PK AI
idProduct           INT FK → product         NOT NULL
idUnit              INT FK → unitOfMeasure   NOT NULL
conversionFactor    DECIMAL(18,6) NOT NULL              -- unidades base = 1 de esta
isBase              BIT NOT NULL DEFAULT 0              -- exactamente 1 por producto
usedForPurchase     BIT NOT NULL DEFAULT 1
usedForSale         BIT NOT NULL DEFAULT 1
codeBarcode         VARCHAR(48) NULL                    -- EAN-8/13, UPC-A; UNIQUE si NOT NULL
namePresentation    NVARCHAR(200) NULL
brandPresentation   NVARCHAR(100) NULL

UQ (idProduct, idUnit)
UQ (codeBarcode) WHERE codeBarcode IS NOT NULL
Invariante: exactamente 1 fila con isBase=1, conversionFactor=1.0, idUnit = product.idUnit


productCategory          -- clasificación de negocio (Salsas, Lácteos…)
productProductCategory   -- M:N product ↔ productCategory
productAccount           -- distribución contable: idProduct → idAccount + idCostCenter + %
```

### Recetas (BOM)

```
productRecipe
─────────────────────────────────────────
idProductRecipe     INT PK AI
idProductOutput     INT FK → product         NOT NULL   -- NO puede ser Materia Prima ni Reventa
nameRecipe          NVARCHAR(200) NOT NULL
quantityOutput      DECIMAL(12,4) NOT NULL              -- cantidad producida en unidad base del output
descriptionRecipe   NVARCHAR(500) NULL
isActive            BIT NOT NULL DEFAULT 1
createdAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE()


productRecipeLine
─────────────────────────────────────────
idProductRecipeLine INT PK AI
idProductRecipe     INT FK → productRecipe   NOT NULL
idProductInput      INT FK → product         NOT NULL   -- ≠ idProductOutput
quantityInput       DECIMAL(12,4) NOT NULL              -- en unidad base del input
sortOrder           INT NOT NULL DEFAULT 0
```

### Inventario por lotes

```
inventoryLot
─────────────────────────────────────────
idInventoryLot      INT PK AI
idProduct           INT FK → product         NOT NULL
lotNumber           VARCHAR(50) NULL          -- proveedor: "{idContact}-{numberInvoice}"
                                              -- producción: código interno "PROD-26032002"
                                              -- ajuste: "SYSTEM-{idInventoryAdjustment}"
expirationDate      DATE NULL                 -- NULL = no perecedero
unitCost            DECIMAL(18,6) NOT NULL DEFAULT 0    -- por unidad base al momento de ingreso
quantityAvailable   DECIMAL(18,6) NOT NULL DEFAULT 0    -- en unidad base; NUNCA editar directo
sourceType          VARCHAR(20) NOT NULL      -- Compra | Producción | Ajuste
idPurchaseInvoice   INT FK → purchaseInvoice  NULL
idInventoryAdjustment INT FK → inventoryAdjustment NULL
idProductionBatch   INT FK → productionBatch  NULL      -- V2; siempre NULL en V1
createdAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE()

IX (idProduct, expirationDate)   -- FEFO queries
IX (idPurchaseInvoice)
```

### Ajustes de inventario

```
inventoryAdjustment
─────────────────────────────────────────
idInventoryAdjustment  INT PK AI
idFiscalPeriod         INT FK → fiscalPeriod  NOT NULL
typeAdjustment         VARCHAR(20) NOT NULL   -- Conteo Físico | Producción | Ajuste de Costo
numberAdjustment       VARCHAR(50) NOT NULL UNIQUE  -- "AJ-YYYYMMDD-NNN" (generado al confirmar)
dateAdjustment         DATE NOT NULL
descriptionAdjustment  NVARCHAR(500) NULL
statusAdjustment       VARCHAR(20) NOT NULL   -- Borrador | Confirmado | Anulado
createdAt              DATETIME2 NOT NULL DEFAULT GETUTCDATE()


inventoryAdjustmentLine
─────────────────────────────────────────
idInventoryAdjustmentLine  INT PK AI
idInventoryAdjustment      INT FK → inventoryAdjustment NOT NULL
idInventoryLot             INT FK → inventoryLot        NOT NULL
quantityDelta              DECIMAL(18,6) NOT NULL   -- + entrada | - salida | 0 solo ajuste costo
unitCostNew                DECIMAL(18,6) NULL        -- si informado: actualiza inventoryLot.unitCost
                                                      -- requerido cuando quantityDelta > 0
descriptionLine            NVARCHAR(500) NULL
```

### Facturas de compra

```
purchaseInvoice
─────────────────────────────────────────
idPurchaseInvoice   INT PK AI
idContact           INT FK → contact         NOT NULL   -- proveedor
idFiscalPeriod      INT FK → fiscalPeriod    NOT NULL
numberInvoice       VARCHAR(…) NOT NULL                 -- número del proveedor (texto libre)
dateInvoice         DATE NOT NULL
statusInvoice       VARCHAR(20) NOT NULL      -- Borrador | Confirmada | Anulada
…campos monetarios, idCurrency, exchangeRateValue…


purchaseInvoiceLine
─────────────────────────────────────────
idPurchaseInvoiceLine INT PK AI
idPurchaseInvoice   INT FK → purchaseInvoice  NOT NULL
idProduct           INT FK → product          NULL     -- NULL = gasto sin producto (flete, etc.)
idUnit              INT FK → unitOfMeasure    NULL     -- debe existir en productUnit para el product
descriptionLine     NVARCHAR(500) NOT NULL
quantity            DECIMAL(18,4) NOT NULL
quantityBase        DECIMAL(18,6) NULL                 -- calculado: quantity × conversionFactor
                                                        -- solo al confirmar; no editable
unitPrice           DECIMAL(18,4) NOT NULL
taxPercent          DECIMAL(5,2) NOT NULL
totalLineAmount     DECIMAL(18,4) NOT NULL
lotNumber           VARCHAR(50) NULL                   -- lote del proveedor (etiqueta física)
expirationDate      DATE NULL                          -- vencimiento según etiqueta del proveedor
```

### Facturas de venta (V1.5)

```
salesInvoice
─────────────────────────────────────────
idSalesInvoice      INT PK AI
idContact           INT FK → contact         NOT NULL   -- cliente
idFiscalPeriod      INT FK → fiscalPeriod    NOT NULL
numberInvoice       VARCHAR(…) NOT NULL                 -- consecutivo autogenerado por el sistema
dateInvoice         DATE NOT NULL
statusInvoice       VARCHAR(20) NOT NULL
…campos monetarios…


salesInvoiceLine
─────────────────────────────────────────
idSalesInvoiceLine  INT PK AI
idSalesInvoice      INT FK → salesInvoice    NOT NULL
idProduct           INT FK → product          NULL
idUnit              INT FK → unitOfMeasure    NULL
descriptionLine     NVARCHAR(500) NOT NULL
quantity            DECIMAL(18,4) NOT NULL
quantityBase        DECIMAL(18,6) NULL                 -- calculado al confirmar
unitPrice           DECIMAL(18,4) NOT NULL
taxPercent          DECIMAL(5,2) NOT NULL
totalLineAmount     DECIMAL(18,4) NOT NULL
```

---

## Diagrama de relaciones

```
unitOfMeasure ◄──────────────────────────────┐
                                              │
productType ◄─────────────────────────────┐  │
                                          │  │
                                       product
                                       ───────
                                       idProductType ───► productType
                                       idUnit        ───► unitOfMeasure
                                       idProductParent ──► product (self, 1 nivel)
                                       averageCost  ← recalculado automáticamente
                                          │
              ┌───────────────────────────┼────────────────────────┐
              │                           │                         │
         productUnit             productProductCategory        productAccount
         ───────────             ──────────────────────        ─────────────
         idProduct ──► product   idProduct ──► product         idProduct ──► product
         idUnit ──► unitOfMeasure idProductCategory ──►        idAccount ──► account
         conversionFactor        productCategory               idCostCenter
         isBase                                                percentageAccount
         codeBarcode (UQ)
         usedForPurchase/Sale

              │                           │                         │
         productRecipe             inventoryLot             purchaseInvoiceLine
         ─────────────             ────────────             ───────────────────
         idProductOutput ─► product idProduct ─► product   idProduct ─► product
         quantityOutput            lotNumber                idUnit ─► unitOfMeasure
         isActive                  expirationDate           quantity
              │                   unitCost                  quantityBase (calculado)
         productRecipeLine         quantityAvailable         lotNumber
         ─────────────────         sourceType               expirationDate
         idProductRecipe ─► recipe idPurchaseInvoice ─►
         idProductInput ─► product purchaseInvoice
         quantityInput             idInventoryAdjustment ─►
                                   inventoryAdjustment
                           inventoryAdjustmentLine
                           ──────────────────────
                           idInventoryAdjustment ─►
                           inventoryAdjustment
                           idInventoryLot ─► inventoryLot
                           quantityDelta
                           unitCostNew
```

---

## Flujo 1 — Compra de Materia Prima

```
1. Crear purchaseInvoice (statusInvoice = 'Borrador')
   └── idContact = proveedor

2. Agregar purchaseInvoiceLine por cada insumo
   ├── Opción A — escaneo de barcode:
   │     SELECT * FROM productUnit WHERE codeBarcode = @ean
   │     → obtiene idProduct + idUnit + conversionFactor + namePresentation
   │     → pre-llena la línea automáticamente
   │
   └── Opción B — selección manual:
         idProduct = producto elegido
         idUnit    = unidad válida en productUnit (usedForPurchase = 1)

3. Registrar en la línea:
   quantity       = cantidad en idUnit del proveedor
   unitPrice      = precio por esa unidad
   lotNumber      = lote impreso en la etiqueta física (opcional)
   expirationDate = vencimiento de la etiqueta (opcional)

4. Confirmar purchaseInvoice → API ejecuta por cada línea con idProduct:

   a) Calcular quantityBase:
      quantityBase = quantity × productUnit.conversionFactor

   b) Calcular unitCost en unidad base:
      unitCostBase = unitPrice ÷ conversionFactor

   c) INSERT inventoryLot:
      idProduct         = línea.idProduct
      lotNumber         = línea.lotNumber       (o "{idContact}-{numberInvoice}" si NULL)
      expirationDate    = línea.expirationDate
      quantityAvailable = línea.quantityBase
      unitCost          = unitCostBase
      sourceType        = 'Compra'
      idPurchaseInvoice = purchaseInvoice.id

   d) Recalcular product.averageCost (costo promedio ponderado):
      averageCost = (stockActual × costoActual + quantityBase × unitCostBase)
                  ÷ (stockActual + quantityBase)

   e) Generar asiento contable vía productAccount:
      DR Inventario / Gasto  (idAccount de productAccount)
      CR Proveedor (idContact)
      CR IVA crédito fiscal (recuperable)
```

---

## Flujo 2 — Paso a Producción (MP → Producto en Proceso)

> En V1 se usa `inventoryAdjustment` tipo `Producción`. En V2 se usa `productionBatch`.

```
1. El operador crea un inventoryAdjustment:
   typeAdjustment        = 'Producción'
   dateAdjustment        = fecha de la corrida
   descriptionAdjustment = 'Corrida Blend Cahuita 03/04/2026'
   statusAdjustment      = 'Borrador'

2. Agregar líneas negativas por cada ingrediente consumido
   (según productRecipeLine de la receta asociada):

   Por cada ingrediente:
     idInventoryLot = lote a consumir (seleccionado por FEFO —ver abajo—)
     quantityDelta  = –cantidad consumida en unidad base del insumo
     unitCostNew    = NULL (no cambia el costo del lote fuente)

   Regla FEFO para seleccionar el lote a consumir:
   ┌─────────────────────────────────────────────────────────┐
   │ SELECT TOP 1 * FROM inventoryLot                        │
   │ WHERE  idProduct = @idProduct                           │
   │   AND  quantityAvailable > 0                            │
   │   AND  (expirationDate IS NULL OR                       │
   │         expirationDate > GETDATE())                     │
   │ ORDER BY                                                │
   │   CASE WHEN expirationDate IS NULL THEN 1 ELSE 0 END,  │
   │   expirationDate ASC,  -- perecederos con venc. cercano │
   │   idInventoryLot ASC   -- desempate: lote más antiguo   │
   └─────────────────────────────────────────────────────────┘
   Si un lote no cubre toda la cantidad requerida,
   consumir el siguiente lote FEFO hasta cubrir el total.

3. Agregar línea positiva para el Producto en Proceso creado:

   idInventoryLot = NUEVO lote (INSERT previo a confirmar)
   quantityDelta  = +quantityOutput de la receta (en unidad base del PP)
   unitCostNew    = propuesto por el API (ver cálculo) o modificado por operador
   lotNumber      = código asignado por la empresa (ej: 'BLEND-20260403')
   expirationDate = NULL o calculado

4. Merma de proceso → línea negativa adicional sobre el lote de entrada:
   quantityDelta  = –unidades perdidas
   descriptionLine = 'Merma de proceso'

5. Confirmar inventoryAdjustment → API ejecuta:

   a) Calcular unitCostNew propuesto (para líneas positivas):
      costoTotal = Σ (|quantityDelta_neg_i| × inventoryLot_i.unitCost)
      unitCostNew = costoTotal ÷ Σ (quantityDelta_pos_j)

      Ejemplo (Blend Cahuita 1000 ML):
        960 ML ingredientes × ¢0.85 promedio = ¢816.00
        40 ML merma         × ¢0.85 promedio = ¢ 34.00
        costoTotal = ¢850.00 → unitCostNew = ¢0.850 / ML

   b) Por cada línea negativa:
      UPDATE inventoryLot SET quantityAvailable += quantityDelta
      (quantityDelta es negativo; verifica que no quede en negativo)

   c) Por cada línea positiva (lote nuevo de PP):
      INSERT inventoryLot (
        sourceType     = 'Producción',
        unitCost       = unitCostNew,
        quantityAvailable = quantityDelta
      )

   d) Recalcular product.averageCost del Producto en Proceso:
      averageCost = unitCostNew   (si es lote inicial)
                  o promedio ponderado si ya había stock previo

   e) Generar asiento contable:
      DR Producto en Proceso  (cuenta del product PP)
      CR Materia Prima insumos consumidos
```

---

## Flujo 3 — Paso a Producto Terminado (PP → PT)

> Mismo mecanismo que Flujo 2, pero los insumos son Productos en Proceso.

```
1. Crear inventoryAdjustment:
   typeAdjustment        = 'Producción'
   descriptionAdjustment = 'Corrida embotellado Cahuita 03/04/2026'

2. Líneas negativas — consumo de PP-BLEND-CAHUITA:
   idInventoryLot = lote FEFO del PP
   quantityDelta  = –960.000 ML  (6 botellas × 160 ML)

3. Línea negativa de merma (opcional):
   quantityDelta  = –40.000 ML
   descriptionLine = 'Merma boquillas'

4. Línea positiva — creación de PT-CAHUITA-160:
   idInventoryLot = NUEVO lote PT
   quantityDelta  = +6.000 BOT160
   lotNumber      = 'PROD-26032002'
   expirationDate = '2027-03-19'
   unitCostNew    = propuesto: ¢850.00 ÷ 6 = ¢141.67 / BOT160

5. Confirmar → misma lógica del Flujo 2:
   - Aplica deltas sobre lotes
   - Recalcula product.averageCost de PT-CAHUITA-160
   - Genera asiento contable DR Producto Terminado / CR Producto en Proceso
```

---

## Flujo 4 — Venta

```
1. Crear salesInvoice (statusInvoice = 'Borrador')
   └── idContact = cliente

2. Agregar salesInvoiceLine:
   ├── Opción A — escaneo de barcode del empaque:
   │     SELECT * FROM productUnit WHERE codeBarcode = @ean
   │     → pre-llena idProduct + idUnit + namePresentation
   │
   └── Opción B — selección manual de producto y unidad
         (usedForSale = 1)

3. Confirmar salesInvoice → API por cada línea con idProduct:

   a) Calcular quantityBase:
      quantityBase = quantity × productUnit.conversionFactor

      Ejemplo: 10 CAJA12 de PT-CAHUITA-160
        quantityBase = 10 × 12 = 120.000 BOT160

   b) Consumir stock por FEFO:
      UPDATE inventoryLot SET quantityAvailable -= quantityBase
      (mismo algoritmo FEFO del Flujo 2 — empezando por lote más próximo a vencer)
      Si el lote no cubre toda la cantidad, pasar al siguiente FEFO.

   c) Verificar stock suficiente antes de confirmar.
      Si stockTotal < quantityBase → error 409.

   d) product.averageCost NO cambia al vender
      (el costo promedio solo cambia al ingresar stock: compra o ajuste positivo)

   e) Generar asiento contable:
      DR Costo de Ventas         (productAccount del PT)
      CR Inventario Producto Terminado
      DR Cliente (idContact)
      CR Ventas
      DR IVA débito fiscal (por pagar)
```

---

## Flujo 5 — Ajuste de Costo

> Corrige el costo de un lote existente **sin mover cantidad** (nota de crédito de proveedor,
> asignación de costos indirectos, corrección de error de registro).

```
1. Crear inventoryAdjustment:
   typeAdjustment        = 'Ajuste de Costo'
   descriptionAdjustment = 'NC proveedor #F-00456 — descuento por volumen'

2. Agregar línea(s) sobre el lote a corregir:
   idInventoryLot = lote existente
   quantityDelta  = 0.000000      ← cero: no mueve stock
   unitCostNew    = nuevo costo   ← requerido; reemplaza inventoryLot.unitCost
   descriptionLine = 'Nota de crédito proveedor Aroy-D'

3. Confirmar → API ejecuta:

   a) inventoryLot.quantityAvailable += 0  (sin cambio)
   b) inventoryLot.unitCost = unitCostNew

   c) Recalcular product.averageCost ponderado con el nuevo unitCost:
      stockExcluyendoLote = SUM(quantityAvailable × unitCost)
                            para todos los lotes excepto el ajustado
      averageCost = (stockExcluyendoLote
                   + inventoryLot.quantityAvailable × unitCostNew)
                   ÷ stockTotal

   d) Generar asiento contable (si afecta ejercicio activo):
      DR / CR Inventario     (diferencia de costo)
      CR / DR Proveedor / Ajuste de costo
```

---

## Flujo 6 — Ajuste de Conteo Físico

> Corrige diferencias entre stock teórico en `inventoryLot` y el conteo físico real.

```
1. Crear inventoryAdjustment:
   typeAdjustment        = 'Conteo Físico'
   descriptionAdjustment = 'Conteo mensual marzo 2026'

2. Por cada diferencia encontrada:

   Si stock teórico > físico (diferencia negativa — merma, robo, error):
     idInventoryLot = lote a ajustar
     quantityDelta  = físico_contado – inventoryLot.quantityAvailable  (< 0)
     unitCostNew    = NULL  (el costo unitario no cambia)

   Si stock teórico < físico (diferencia positiva — error de registro previo):
     Opción A: ajustar lote existente → quantityDelta > 0
     Opción B: crear lote nuevo con sourceType = 'Ajuste'
       lotNumber = 'SYSTEM-{idInventoryAdjustment}'
       quantityDelta = +unidades faltantes
       unitCostNew   = costo a asignar al stock sin origen claro

3. Confirmar → mismo mecanismo de Flujo 2 paso 5:
   - Aplica deltas
   - Recalcula averageCost si hay línea positiva con unitCostNew
   - Genera asiento: DR/CR Inventario vs. Gasto por diferencia de inventario
```

---

## Cálculo del Costo Promedio Ponderado

El campo `product.averageCost` representa el **costo por unidad base** al momento actual.
Se recalcula en tres eventos:

| Evento | Fórmula |
|---|---|
| Confirmar `purchaseInvoice` | `(stockPrevio × costoAnterior + quantityBase × unitCostNuevo) ÷ (stockPrevio + quantityBase)` |
| Confirmar `inventoryAdjustment` con `quantityDelta > 0` | Ídem con los valores de la(s) línea(s) positiva(s) |
| Confirmar `inventoryAdjustment` con `quantityDelta = 0` y `unitCostNew` informado | Recalculo ponderado reemplazando el `unitCost` del lote afectado |
| Confirmar `salesInvoice` | **No cambia** — el costo promedio no se altera al vender |

```
Ejemplo numérico — MP-LECHE-COCO:

Estado inicial: 5000 ML @ ¢0.003125/ML (averageCost = ¢0.003125)

Nueva compra: 30 latas LATA400 × ¢1.25/lata
  quantityBase    = 30 × 400 = 12000 ML
  unitCostBase    = ¢1.25 ÷ 400 = ¢0.003125/ML

  averageCost nuevo = (5000 × 0.003125 + 12000 × 0.003125) ÷ (5000 + 12000)
                    = ¢0.003125/ML  (sin cambio porque el costo es idéntico)

Nota de crédito del proveedor → lote ajustado a ¢0.0030/ML:
  inventoryLot.unitCost = 0.0030
  averageCost = (3000 ML (otros lotes) × 0.003125 + 12000 ML (lote NC) × 0.0030)
              ÷ 15000
              = ¢0.003025/ML
```

---

## Selección de Lote FEFO

Aplica en: **Flujo 2** (consumo en producción), **Flujo 3** (embotellado), **Flujo 4** (ventas).

```sql
-- Selecciona el siguiente lote a consumir para @idProduct
SELECT TOP 1
    idInventoryLot,
    lotNumber,
    expirationDate,
    unitCost,
    quantityAvailable
FROM   inventoryLot
WHERE  idProduct = @idProduct
  AND  quantityAvailable > 0
  AND  (expirationDate IS NULL OR expirationDate > GETDATE())
ORDER BY
    CASE WHEN expirationDate IS NULL THEN 1 ELSE 0 END ASC,
    -- 0 = perecedero (consume primero), 1 = no perecedero (consume después)
    expirationDate ASC,     -- vence antes: consume primero
    idInventoryLot ASC      -- mismo vencimiento: lote más antiguo
```

Si el lote seleccionado no cubre toda la cantidad requerida:
```
consumir = MIN(quantityAvailable_lote, cantidadRestante)
UPDATE inventoryLot SET quantityAvailable -= consumir WHERE idInventoryLot = @id
cantidadRestante -= consumir
-- repetir con el siguiente lote FEFO hasta cantidadRestante = 0
```

Si tras agotar todos los lotes sigue `cantidadRestante > 0` → error: **stock insuficiente**.
Si existen lotes vencidos (`expirationDate ≤ GETDATE()`) → alerta: **stock vencido disponible**.

---

## Resumen de recursos que generan / consumen `inventoryLot`

| Evento | Acción sobre `inventoryLot` | Actualiza `averageCost` |
|---|---|---|
| Confirmar `purchaseInvoice` | INSERT fila nueva por cada línea con `idProduct` | Sí |
| Confirmar `inventoryAdjustment` tipo `Producción` | UPDATE (líneas –) e INSERT o UPDATE (líneas +) | Sí (líneas +) |
| Confirmar `inventoryAdjustment` tipo `Conteo Físico` | UPDATE `quantityAvailable` por delta | Solo si línea positiva con `unitCostNew` |
| Confirmar `inventoryAdjustment` tipo `Ajuste de Costo` | UPDATE `unitCost` (`quantityDelta = 0`) | Sí (recalculo ponderado) |
| Confirmar `salesInvoice` | UPDATE `quantityAvailable` via FEFO | No |
| Anular cualquier documento | Reversión del delta original (o bloqueo si el stock ya fue consumido) | Sí si la reversión mueve stock |

---

## Restricciones de negocio — resumen

| Regla | Detalle |
|---|---|
| `productType` Materia Prima / Reventa | No puede ser `idProductOutput` en ninguna receta |
| `product.idProductType` cambio | Bloqueado si el producto aparece en alguna `productRecipe` (activa o no). Error 409. |
| `idProductParent` jerarquía | Máximo un nivel: un padre no puede tener padre. |
| `productUnit.isBase` | Exactamente 1 por producto; `conversionFactor` debe ser `1.0`; `idUnit` debe coincidir con `product.idUnit` |
| `productUnit.codeBarcode` | UNIQUE en todo el sistema (un EAN identifica exactamente 1 presentación de 1 producto) |
| `purchaseInvoiceLine.idUnit` | Debe existir en `productUnit` para ese `idProduct` con `usedForPurchase = 1` |
| `inventoryLot.quantityAvailable` | Nunca puede quedar negativo. El API rechaza antes de aplicar. |
| `inventoryAdjustmentLine` con `quantityDelta > 0` | `unitCostNew` es requerido |
| `inventoryAdjustmentLine` con `quantityDelta = 0` | Sin `unitCostNew` la línea no tiene efecto (el API debería advertir) |
| `productRecipeLine.idProductInput` | No puede ser igual al `idProductOutput` de la misma receta |
