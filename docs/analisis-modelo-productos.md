# Análisis y Rediseño del Modelo de Productos

> Estado: **Borrador v3 — pendiente de revisión y aprobación**
> Fecha: 3 de abril de 2026

---

## 1. Estado Actual — Inventario de Tablas

| Tabla | Descripción | Campos clave |
|---|---|---|
| `product` | Catálogo interno | `idProduct`, `codeProduct`, `nameProduct` |
| `productSKU` | SKUs de código de barras (EAN/UPC) | `idProductSKU`, `codeProductSKU`, `nameProductSKU`, `brandProductSKU`, `netContent` (string libre) |
| `productProductSKU` | Relación M:N product ↔ productSKU | join table |
| `productCategory` | Categorías de clasificación | `idProductCategory`, `nameProductCategory` |
| `productProductCategory` | Relación M:N product ↔ productCategory | join table |
| `productAccount` | Distribución contable por producto | `idProduct`, `idAccount`, `idCostCenter`, `percentageAccount` |
| `purchaseInvoiceLine` | Línea de factura de compra | `idProductSKU` (FK nullable) |

---

## 2. Problemas Identificados

### 2.1 Sin tipo de producto → ambigüedad total
`product` no distingue entre Materia Prima, Producto en Proceso ni Producto Terminado. No es posible filtrar por fase productiva ni aplicar reglas de negocio distintas por fase.

### 2.2 Sin unidad de medida estructurada
- `product` no tiene unidad de medida.
- `productSKU.netContent` es un string libre ("500ml", "1kg") → no permite cálculos ni validaciones.

### 2.3 No existe modelo de transformación (receta / BOM)
No hay ninguna tabla que exprese "para fabricar X unidades del producto A se necesitan Y gramos del insumo B". Sin esto es imposible calcular costos de producción, planificar consumo de materias primas o rastrear inventario por fase.

### 2.4 Inconsistencia contable: SKU ↔ cuentas
`purchaseInvoiceLine` referencia `idProductSKU`, pero `productAccount` vive en `product`. Para generar el asiento contable hay que atravesar la M:N `productProductSKU`. Si un SKU está vinculado a más de un `product`, el sistema no puede elegir a cuál aplicar → ambigüedad irresolvible.

### 2.5 Sin inventario / stock
No existe ninguna tabla de inventario. Se registra aquí como deuda técnica inmediata.

### 2.6 `productCategory` puede confundirse con tipo
Alguien podría crear categorías llamadas "Materia Prima" usando `productCategory`, duplicando la semántica de tipo. Separar: **tipo = naturaleza productiva**, **categoría = clasificación de negocio** (Salsas, Lácteos, etc.).

### 2.7 Un producto puede comprarse/venderse en múltiples unidades
La leche de coco se puede comprar en lata de 400 ml, galón o barril. Sin un catálogo de unidades por producto con factores de conversión no es posible registrar facturas en distintas presentaciones ni consolidar el inventario en una unidad canónica.

### 2.8 `productSKU` y `productProductSKU` son redundantes — deben eliminarse

Este es el problema de diseño más importante.

`productSKU` modela: *una presentación comercial identificada por código de barras con contenido neto*.

`productUnit` (propuesto en §4.5) modela: *una unidad de compra/venta de un producto con su factor de conversión a la unidad base*.

**Son exactamente lo mismo.** Una lata de leche de coco Aroy-D 400 ml es simultáneamente:
- Una *unidad de compra* del producto "Leche de Coco" con `conversionFactor = 400` ML.
- Una *presentación con barcode* EAN-13.

No tiene sentido mantener estas dos identidades en tablas separadas. Consecuencias del modelo actual:

| Situación | Problema |
|---|---|
| Comprar leche de coco a granel | `purchaseInvoiceLine` solo con `idProduct`, sin `idProductSKU` — inconsistencia de uso de la tabla |
| Comprar en lata con EAN | `purchaseInvoiceLine` necesita tanto `idProductSKU` como `idProduct` para llegar a `productAccount` |
| Calcular ML recibidos al inventario | Hay que parsear `netContent` (string). Imposible automáticamente. |
| Escanear barcode en recepción | Hay que cruzar `productSKU` → M:N → `product` en 2 tablas |

Al fusionar en `productUnit` con campo `codeBarcode` opcional:

| Situación | Con la nueva propuesta |
|---|---|
| Comprar a granel | `idProduct` + `idUnit` base, `codeBarcode = NULL` |
| Comprar en lata con EAN | `idProduct` + `idUnit` LATA400, `codeBarcode` en `productUnit` |
| Calcular ML al inventario | `quantity × productUnit.conversionFactor` |
| Escanear barcode en recepción | `productUnit.codeBarcode = EAN` → obtiene `idProduct` + `conversionFactor` en un solo paso |

**Decisión: eliminar `productSKU` y `productProductSKU`. Sus responsabilidades se absorben en `productUnit`.**

### 2.9 Sin trazabilidad de lotes ni rotación FEFO

El modelo actual no tiene ninguna tabla de lotes de inventario. Para productos perecederos (salsas, alimentos, insumos biológicos) esto es crítico:

- Sin lotes no es posible aplicar **FEFO** (First Expired, First Out): sin esta rotación se puede usar en producción ingredientes recién comprados mientras los más antiguos y próximos a vencer se deterioran.
- Sin número de lote del proveedor no hay trazabilidad hacia atrás: ¿cuáles latas de leche de coco entraron al lote de salsa `26032002`?
- Al recepcionar una compra el documento físico del proveedor incluye lote y fecha de vencimiento — esos datos deben registrarse en la línea de factura y trasladarse automáticamente al inventario.
- Al producir, la corrida genera su propio número de lote (`26032002`) con su propia fecha de vencimiento (`19.03.27`).

**Decisión: `inventoryLot` se promueve de deuda técnica a V1 del presente rediseño.** Ver §4.13.

---

## 3. Dos Verticales del Negocio

```
VERTICAL A — Manufactura (ejemplo: Cahuita Salsa Caribeña)
  Compra de Materias Primas en múltiples presentaciones
       ↓
  Transformación → Producto en Proceso (mezcla antes de embotellar)
       ↓
  Transformación → Producto Terminado (botella 160ml con barcode propio)
       ↓
  Venta

VERTICAL B — Reventa
  Compra de Producto Terminado de terceros (con barcode del fabricante)
       ↓
  Reventa directa (sin transformación)
```

Las dos verticales comparten las mismas tablas. La diferencia está en el `productType` y en la presencia o ausencia de receta.

---

## 4. Propuesta de Modelo Rediseñado

### 4.1 Diagrama de relaciones

```
unitOfMeasure ─────────────────────────────────┐
                                                ├──< product >──── productType
productType ────────────────────────────────────┘        │
                                                          │ 1:N
                                                  productUnit ──── unitOfMeasure
                                                  (codeBarcode opcional)
                                                          │
                                                  productProductCategory >── productCategory
                                                          │
                                                  productAccount >── account / costCenter
                                                          │
                                                  productRecipe (output=product)
                                                          │
                                                  productRecipeLine >── product (input)

purchaseInvoiceLine ──> product  (idProduct)
                    ──> unitOfMeasure (idUnit, validado contra productUnit)
```

**Tablas del modelo actual eliminadas:** `productSKU`, `productProductSKU`.

---

### 4.2 `unitOfMeasure` — NUEVA

Catálogo global de unidades de medida.

| Campo | Tipo | Descripción |
|---|---|---|
| `idUnit` | INT PK AI | |
| `codeUnit` | VARCHAR(10) UNIQUE NOT NULL | Código corto: `ML`, `GR`, `KG`, `LTR`, `BOT160`… |
| `nameUnit` | NVARCHAR(80) NOT NULL | Nombre legible: "Mililitro", "Gramo", "Botella 160ml" |
| `typeUnit` | VARCHAR(20) NOT NULL | `Volumen` \| `Masa` \| `Unidad` \| `Longitud` |

**Datos de ejemplo:**

| `codeUnit` | `nameUnit` | `typeUnit` |
|---|---|---|
| `ML` | Mililitro | Volumen |
| `LTR` | Litro | Volumen |
| `GR` | Gramo | Masa |
| `KG` | Kilogramo | Masa |
| `UNI` | Unidad genérica | Unidad |
| `BOT160` | Botella 160ml | Unidad |
| `LATA400` | Lata 400ml | Unidad |
| `LATA1000` | Lata 1000ml | Unidad |
| `SAC1K` | Saco 1kg | Unidad |

> `BOT160` y `LATA400` son unidades de presentación. Su equivalencia en ML la define cada producto en `productUnit.conversionFactor`.

---

### 4.3 `productType` — NUEVA

Tipo según la fase productiva. Catálogo fijo de sistema, sin CRUD expuesto al usuario.

| Campo | Tipo | Descripción |
|---|---|---|
| `idProductType` | INT PK AI | |
| `nameProductType` | NVARCHAR(60) UNIQUE NOT NULL | |
| `descriptionProductType` | NVARCHAR(300) NULL | |

| `nameProductType` | Descripción |
|---|---|
| `Materia Prima` | Insumo comprado externamente. Ej: leche de coco, chile habanero, sal |
| `Producto en Proceso` | Resultado intermedio de transformación. Ej: mezcla de salsa antes de embotellar |
| `Producto Terminado` | Producto final listo para venta. Ej: botella Cahuita 160ml etiquetada |
| `Reventa` | Producto de terceros comprado listo y revendido sin transformación |

---

### 4.4 `product` — MODIFICADA

| Campo | Tipo | ¿Nuevo? | Descripción |
|---|---|---|---|
| `idProduct` | INT PK AI | — | |
| `codeProduct` | VARCHAR(50) UNIQUE NOT NULL | — | Código interno |
| `nameProduct` | NVARCHAR(200) NOT NULL | — | Nombre interno |
| `idProductType` | INT FK NOT NULL | **✓ NUEVO** | FK → `productType` |
| `idUnit` | INT FK NOT NULL | **✓ NUEVO** | FK → `unitOfMeasure` — **unidad base** para inventario y recetas. Debe tener un `productUnit` con `isBase=1` y esta misma unidad. |
| `idProductParent` | INT FK NULL | **✓ NUEVO** | FK auto-referencial → `product`. Agrupa variantes del mismo producto (ej: Cahuita 160ml y Cahuita 500ml bajo "Cahuita Salsa Caribeña"). NULL si es producto raíz o no tiene familia. Un producto padre no debe tener `idProductParent`. |
| `averageCost` | DECIMAL(18,6) NOT NULL DEFAULT 0 | **✓ NUEVO** | Costo promedio ponderado en **unidad base**. Se recalcula automáticamente al confirmar cada `purchaseInvoice` o `inventoryAdjustment` que suma stock. |

**Restricciones de negocio:**
- Tipos `Materia Prima` y `Reventa` no pueden ser `idProductOutput` en una receta.
- Cambio de `idProductType` solo permitido si el producto no aparece en ninguna `productRecipe` (activa o inactiva). Error 409 si tiene recetas.
- `idProductParent` no puede apuntar a un producto que ya tenga `idProductParent` (máximo un nivel de jerarquía).

---

### 4.5 `productUnit` — NUEVA (absorbe `productSKU` y `productProductSKU`)

Catálogo de presentaciones válidas por producto: unidad, factor de conversión a la base, y opcionalmente el código de barras del empaque. **Esta tabla reemplaza por completo `productSKU` + `productProductSKU`.**

| Campo | Tipo | Descripción |
|---|---|---|
| `idProductUnit` | INT PK AI | |
| `idProduct` | INT FK NOT NULL | FK → `product` |
| `idUnit` | INT FK NOT NULL | FK → `unitOfMeasure` |
| `conversionFactor` | DECIMAL(18,6) NOT NULL | Cuántas **unidades base** equivale 1 de esta presentación. La fila base siempre vale `1.000000`. |
| `isBase` | BIT NOT NULL DEFAULT 0 | Exactamente 1 registro por producto. Debe coincidir con `product.idUnit`. |
| `usedForPurchase` | BIT NOT NULL DEFAULT 1 | Puede usarse en líneas de factura de compra |
| `usedForSale` | BIT NOT NULL DEFAULT 1 | Puede usarse en líneas de factura de venta (futuro) |
| `codeBarcode` | VARCHAR(48) NULL | EAN-8, EAN-13, UPC-A… NULL si es unidad interna sin empaque |
| `namePresentation` | NVARCHAR(200) NULL | Nombre en el empaque. Ej: "Cahuita Salsa Caribeña 160ml" |
| `brandPresentation` | NVARCHAR(100) NULL | Marca del fabricante del empaque. Ej: "Fiesta de Diablitos", "Aroy-D" |

**Índices:**
- `UQ_productUnit_idProduct_idUnit` sobre `(idProduct, idUnit)`.
- `UQ_productUnit_codeBarcode` sobre `codeBarcode` WHERE `codeBarcode IS NOT NULL` — un EAN identifica una única fila en todo el sistema.

**Invariante:** exactamente una fila por producto con `isBase=1`, `conversionFactor=1.000000`, y `idUnit = product.idUnit`.

**Flujo de escaneo en recepción de mercadería:**
```
Escanear EAN
→ SELECT * FROM productUnit WHERE codeBarcode = @ean
→ Obtiene: idProduct + idUnit + conversionFactor + namePresentation
→ Pre-llena la línea de factura automáticamente
```

---

### 4.6 `productSKU` — **ELIMINADA**

Ver §2.8. Sus campos se absorben en `productUnit`:
- `codeProductSKU` → `productUnit.codeBarcode`
- `nameProductSKU` → `productUnit.namePresentation`
- `brandProductSKU` → `productUnit.brandPresentation`
- `netContent` (string libre) → `productUnit.conversionFactor` (decimal exacto) + `productUnit.idUnit`

---

### 4.7 `productProductSKU` — **ELIMINADA**

Join table innecesaria. Relación barcode ↔ producto es ahora directa: `productUnit.codeBarcode`.

---

### 4.8 `productCategory` — SIN CAMBIOS

Clasificación de negocio (Salsas, Salsas Picantes, Artesanales…). No confundir con `productType`. Un producto puede pertenecer a múltiples categorías.

---

### 4.9 `productAccount` — SIN CAMBIOS

Distribución contable por producto, intacta. La referencia directa `purchaseInvoiceLine.idProduct` → `productAccount` elimina la ambigüedad del §2.4.

---

### 4.10 `purchaseInvoiceLine` — MODIFICADA

`idProductSKU` desaparece. Se añaden `idProduct`, `idUnit` y `quantityBase`.

| Campo | Tipo | ¿Cambio? | Descripción |
|---|---|---|---|
| `idPurchaseInvoiceLine` | INT PK AI | — | |
| `idPurchaseInvoice` | INT FK NOT NULL | — | |
| ~~`idProductSKU`~~ | ~~INT FK NULL~~ | **✗ ELIMINADO** | Reemplazado por `idProduct` + `idUnit` |
| `idProduct` | INT FK NULL | **✓ NUEVO** | FK → `product`. NULL para líneas de gasto sin producto (flete, servicio) |
| `idUnit` | INT FK NULL | **✓ NUEVO** | FK → `unitOfMeasure`. Debe existir en `productUnit` para ese producto. NULL = usa unidad base. |
| `descriptionLine` | NVARCHAR(500) NOT NULL | — | Descripción libre de la línea |
| `quantity` | DECIMAL(18,4) NOT NULL | — | Cantidad en la unidad `idUnit` |
| `quantityBase` | DECIMAL(18,6) NULL | **✓ NUEVO** | `quantity × productUnit.conversionFactor`. Calculado al confirmar la factura. Alimenta el inventario. |
| `unitPrice` | DECIMAL(18,4) NOT NULL | — | Precio por unidad de `idUnit` |
| `taxPercent` | DECIMAL(5,2) NOT NULL | — | |
| `totalLineAmount` | DECIMAL(18,4) NOT NULL | — | |
| `lotNumber` | VARCHAR(50) NULL | **✓ NUEVO** | Número de lote del proveedor impreso en la etiqueta del insumo. Se registra al recepcionar. Pasa a `inventoryLot.lotNumber` al confirmar la factura. |
| `expirationDate` | DATE NULL | **✓ NUEVO** | Fecha de vencimiento según etiqueta del proveedor. Pasa a `inventoryLot.expirationDate`. NULL para productos no perecederos. |

**Reglas:**
- `idUnit` debe existir en `productUnit` para el `idProduct` dado.
- `quantityBase` lo calcula el API al confirmar (`quantity × productUnit.conversionFactor`); no es editable.
- Una línea sin `idProduct` no genera movimiento de inventario (gasto de servicio, flete, etc.).
- Al confirmar la factura, por cada línea con `idProduct` se crea automáticamente un registro en `inventoryLot`.

---

### 4.11 `productRecipe` — NUEVA

| Campo | Tipo | Descripción |
|---|---|---|
| `idProductRecipe` | INT PK AI | |
| `idProductOutput` | INT FK NOT NULL | FK → `product` — el producto que produce esta receta |
| `nameRecipe` | NVARCHAR(200) NOT NULL | Nombre descriptivo |
| `quantityOutput` | DECIMAL(12,4) NOT NULL | Cantidad producida por corrida, en la **unidad base** del output |
| `descriptionRecipe` | NVARCHAR(500) NULL | Instrucciones generales, observaciones |
| `isActive` | BIT NOT NULL DEFAULT 1 | Solo recetas activas se usan en producción |
| `createdAt` | DATETIME2 NOT NULL DEFAULT GETUTCDATE() | |

**Restricción:** `idProductOutput` no puede ser de tipo `Materia Prima` ni `Reventa`.

---

### 4.12 `productRecipeLine` — NUEVA

Ingredientes de la receta, siempre en **unidad base** del insumo.

| Campo | Tipo | Descripción |
|---|---|---|
| `idProductRecipeLine` | INT PK AI | |
| `idProductRecipe` | INT FK NOT NULL | FK → `productRecipe` |
| `idProductInput` | INT FK NOT NULL | FK → `product` — insumo requerido |
| `quantityInput` | DECIMAL(12,4) NOT NULL | Cantidad en la unidad base de `idProductInput` |
| `sortOrder` | INT NOT NULL DEFAULT 0 | Orden de visualización |

**Restricción:** `idProductInput ≠ idProductOutput` de la receta padre (sin auto-referencias).

> **Sobre conversiones de unidad durante la producción:** El modelo no requiere una tabla de conversión entre tipos de unidad (GR → ML, sólidos → líquido). La receta **es** la conversión: expresa físicamente que "600 ML de leche de coco + 80 GR de chile habanero + otros ingredientes → 1000 ML de mezcla". Cada input se registra en la unidad base de su propio producto; el output en la unidad base del producto resultante. La transformación inter-tipos de unidad está capturada experimentalmente en los valores de `quantityInput` y `quantityOutput`. No hay conversión matemática implícita — esto es deliberado: la física del proceso la define la receta, no el sistema.

---

### 4.13 `inventoryLot` — NUEVA (promovida a V1)

Registro del saldo de inventario por producto y lote. Un lote puede originarse de una compra (materia prima o producto para reventa) o de una corrida de producción (producto en proceso o terminado). Es la unidad mínima de trazabilidad y la base para la rotación FEFO.

| Campo | Tipo | Descripción |
|---|---|---|
| `idInventoryLot` | INT PK AI | |
| `idProduct` | INT FK NOT NULL | FK → `product` |
| `lotNumber` | VARCHAR(50) NULL | Número de lote: para compras se genera como `{idContact}-{numberInvoice}` (ej: `42-F-00123`). Para producción: código generado internamente. Para ajustes: `SYSTEM-{idInventoryAdjustment}`. |
| `expirationDate` | DATE NULL | Fecha de vencimiento. NULL para productos no perecederos. |
| `unitCost` | DECIMAL(18,6) NOT NULL DEFAULT 0 | Costo unitario en **unidad base** al momento del ingreso. Se usa para recalcular `product.averageCost`. |
| `quantityAvailable` | DECIMAL(18,6) NOT NULL DEFAULT 0 | Stock disponible en **unidad base** del producto. Solo se modifica al confirmar documentos (`purchaseInvoice`, `salesInvoice`, `inventoryAdjustment`). **Nunca editable directamente.** |
| `sourceType` | VARCHAR(20) NOT NULL | `Compra` \| `Producción` \| `Ajuste` |
| `idPurchaseInvoice` | INT FK NULL | FK → `purchaseInvoice`. Poblado si `sourceType = 'Compra'`. |
| `idInventoryAdjustment` | INT FK NULL | FK → `inventoryAdjustment`. Poblado si `sourceType = 'Ajuste'`. |
| `idProductionBatch` | INT FK NULL | FK → `productionBatch` (V2). Poblado si `sourceType = 'Producción'`. NULL en V1. |
| `createdAt` | DATETIME2 NOT NULL DEFAULT GETUTCDATE() | |

**Índices:**
- `IX_inventoryLot_idProduct_expiration` sobre `(idProduct, expirationDate)` — soporte para queries FEFO.
- `IX_inventoryLot_idPurchaseInvoice` sobre `idPurchaseInvoice`.

**Stock total por producto:** `SELECT SUM(quantityAvailable) FROM inventoryLot WHERE idProduct = @id`

**Regla FEFO — selección del lote a consumir:**
```sql
SELECT TOP 1 *
FROM   inventoryLot
WHERE  idProduct = @idProduct
  AND  quantityAvailable > 0
  AND  (expirationDate IS NULL OR expirationDate > GETDATE())
ORDER BY
  CASE WHEN expirationDate IS NULL THEN 1 ELSE 0 END, -- perecederos primero
  expirationDate ASC,
  idInventoryLot ASC  -- desempate: lote más antiguo
```

> Los lotes sin fecha de vencimiento se consumen después de los perecederos. Si un lote ya venció (`expirationDate ≤ GETDATE()`) no se selecciona y el API debería alertar que hay stock vencido.

**Flujo de los tres eventos que crean o consumen lotes:**

**① Confirmar `purchaseInvoice`** (automático en el API):
```
Por cada purchaseInvoiceLine con idProduct ≠ NULL:
  INSERT inventoryLot (
    idProduct         = línea.idProduct,
    lotNumber         = línea.lotNumber,       -- del proveedor
    expirationDate    = línea.expirationDate,  -- del proveedor
    quantityAvailable = línea.quantityBase,    -- ya convertida a unidad base
    sourceType        = 'Compra',
    idPurchaseInvoice = línea.idPurchaseInvoice
  )
```

**② Producción — V1 simplificado** (el operador registra la corrida manualmente):
```
Por cada ingrediente consumido:
  FEFO → UPDATE inventoryLot SET quantityAvailable -= cantidadUsadaBase
         (empezando por el lote de expirationDate más próxima)

Por el producto output:
  INSERT inventoryLot (
    idProduct         = idProductOutput,
    lotNumber         = "26032002",  -- asignado por la empresa
    expirationDate    = 2027-03-19,  -- calculado por la empresa
    quantityAvailable = quantityOutput,  -- en unidad base del output
    sourceType        = 'Producción'
  )
```
> En V2, `productionBatch` automatiza este flujo y registra la trazabilidad exacta de qué lotes de insumos se consumieron.

**③ Confirmar `salesInvoice`** (automático en el API):
```
Por cada salesInvoiceLine con idProduct ≠ NULL:
  FEFO → UPDATE inventoryLot SET quantityAvailable -= línea.quantityBase
  Recalcular product.averageCost (el costo no cambia al vender, solo al comprar/ajustar)
```

---

### 4.14 `inventoryAdjustment` — NUEVA (V1)

Documento de ajuste de inventario. Cubre tres casos de uso:

| `typeAdjustment` | Uso |
|---|---|
| `Conteo Físico` | Corrección entre stock teórico y conteo físico real |
| `Producción` | Corrida de producción manual en V1: consume insumos/PP y genera PP/PT |
| `Ajuste de Costo` | Corrección del costo unitario de un lote sin mover cantidades (ej: nota de crédito de proveedor, asignación de costos indirectos) |

Es el único mecanismo válido para modificar `inventoryLot.quantityAvailable` fuera de una factura. En V2, el tipo `Producción` evolucionará a `productionBatch` con trazabilidad completa de lotes consumidos.

| Campo | Tipo | Descripción |
|---|---|---|
| `idInventoryAdjustment` | INT PK AI | |
| `idFiscalPeriod` | INT FK NOT NULL | FK → `fiscalPeriod` |
| `typeAdjustment` | VARCHAR(20) NOT NULL | `Conteo Físico` \| `Producción` \| `Ajuste de Costo` |
| `numberAdjustment` | VARCHAR(50) NOT NULL UNIQUE | Consecutivo interno generado al confirmar. Formato `AJ-YYYYMMDD-NNN`. |
| `dateAdjustment` | DATE NOT NULL | Fecha del evento (conteo, corrida, ajuste). |
| `descriptionAdjustment` | NVARCHAR(500) NULL | Motivo (ej: "Conteo físico mensual", "Corrida lote 26032002", "NC proveedor #F-00456"). |
| `statusAdjustment` | VARCHAR(20) NOT NULL | `Borrador` \| `Confirmado` \| `Anulado` |
| `createdAt` | DATETIME2 NOT NULL DEFAULT GETUTCDATE() | |

### 4.15 `inventoryAdjustmentLine` — NUEVA (V1)

Líneas del ajuste. Cada línea referencia un lote específico, establece la cantidad **delta** (puede ser cero en ajustes de costo) y opcionalmente el nuevo costo unitario.

| Campo | Tipo | Descripción |
|---|---|---|
| `idInventoryAdjustmentLine` | INT PK AI | |
| `idInventoryAdjustment` | INT FK NOT NULL | FK → `inventoryAdjustment` |
| `idInventoryLot` | INT FK NOT NULL | FK → `inventoryLot` — el lote a ajustar. Para líneas positivas que crean un lote nuevo, se crea el `inventoryLot` primero y se referencia aquí. |
| `quantityDelta` | DECIMAL(18,6) NOT NULL | Diferencia en **unidad base**. Positivo = entrada; negativo = salida; **cero = ajuste de costo puro** (no mueve stock). |
| `unitCostNew` | DECIMAL(18,6) NULL | Nuevo costo unitario para el lote. Si NULL, no se modifica el costo. Requerido en líneas que crean stock nuevo (`quantityDelta > 0`). Usado también en ajuste de costo puro (`quantityDelta = 0`). |
| `descriptionLine` | NVARCHAR(500) NULL | Detalle por línea. |

**Reglas al confirmar:**
- `inventoryLot.quantityAvailable += quantityDelta` por cada línea.
- Si el resultado es negativo, el API rechaza con error.
- Si `unitCostNew` está informado: `inventoryLot.unitCost = unitCostNew`.
- Si `quantityDelta > 0` o `unitCostNew` está informado: recalcular `product.averageCost` ponderado.
- Si se crea un lote nuevo: `inventoryLot.sourceType = 'Producción'` cuando `typeAdjustment = 'Producción'`; `sourceType = 'Ajuste'` en los otros casos.

**Cálculo automático de `unitCostNew` en ajuste tipo `Producción`:**

Para cada línea positiva (`quantityDelta > 0`), el API propone automáticamente:

```
costoTotal = Σ (|quantityDelta_negativa_i| × inventoryLot_i.unitCost)  -- suma de todas las líneas negativas del mismo ajuste
unitCostNew_propuesto = costoTotal ÷ Σ(quantityDelta_positiva_j)       -- dividido entre el total de unidades base creadas
```

Ejemplo con la corrida de embotellado:
```
Líneas negativas:
  960 ML de PP-BLEND-CAHUITA × ¢0.85/ML = ¢816.00
  40 ML merma                × ¢0.85/ML = ¢34.00
  costoTotal = ¢850.00

Líneas positivas:
  +6 BOT160 de PT-CAHUITA-160
  unitCostNew_propuesto = ¢850.00 ÷ 6 = ¢141.67 / BOT160
```

El operador puede aceptar el valor propuesto o sobreescribir `unitCostNew` antes de confirmar. Si lo modifica, el sistema no valida la diferencia (puede representar costos indirectos no capturados en el ajuste).

**Flujo de producción en V1 — un único documento tipo `Producción`:**

```
Ejemplo: corrida que consume 1000 ML de PP-BLEND-CAHUITA y produce 6 BOT160 de PT-CAHUITA-160

inventoryAdjustment:
  typeAdjustment     = 'Producción'
  descriptionAdjust  = 'Corrida embotellado 03/04/2026'

Línea 1 (consumo de Producto en Proceso):
  idInventoryLot     = lote del PP-BLEND-CAHUITA existente
  quantityDelta      = -960.000 ML      ← salida (según receta: 6 × 160ml)
  unitCostNew        = NULL

Línea 2 (creación de Producto Terminado — lote nuevo):
  idInventoryLot     = NUEVO lote PT-CAHUITA-160 (sourceType='Producción')
  quantityDelta      = +6.000 BOT160    ← entrada
  unitCostNew        = costo calculado por el operador o derivado del averageCost del PP
  lotNumber          = 'PROD-26032002'
  expirationDate     = '2027-03-19'
```

> Los ~40 ML de merma de boquillas se registran como una segunda línea negativa sobre el lote de PP-BLEND-CAHUITA con `quantityDelta = -40.000` y `descriptionLine = 'Merma de proceso'`.

---

## 5. Ejemplo Concreto — Cahuita Salsa Caribeña (Fiesta de Diablitos)

> **Empresa:** Corporación Los Diablitos SRL, Desamparados, Costa Rica — fabrica esta salsa.
> **EAN-13 en botella:** `7443036860195` — código propio de la empresa impreso en su producto terminado.
> **Lote `26032002` / Vencimiento `19.03.27`** — datos de la corrida de producción, se gestionarán en `productionBatch` (V2).

---

### 5.1 Catálogo `unitOfMeasure` (para este caso)

| `codeUnit` | `nameUnit` | `typeUnit` |
|---|---|---|
| `ML` | Mililitro | Volumen |
| `LTR` | Litro | Volumen |
| `GR` | Gramo | Masa |
| `KG` | Kilogramo | Masa |
| `BOT160` | Botella 160ml | Unidad |
| `LATA400` | Lata 400ml | Unidad |
| `SAC1K` | Saco 1kg | Unidad |

---

### 5.2 Catálogo `product`

| `codeProduct` | `nameProduct` | `productType` | Unidad base |
|---|---|---|---|
| `MP-LECHE-COCO` | Leche de Coco | Materia Prima | ML |
| `MP-SAL` | Sal | Materia Prima | GR |
| `MP-CHILE-HABANERO` | Chile Habanero | Materia Prima | GR |
| `MP-HIERBAS-FRESCAS` | Hierbas Frescas (mezcla) | Materia Prima | GR |
| `MP-CURRY` | Curry en Polvo | Materia Prima | GR |
| `MP-AJO` | Ajo | Materia Prima | GR |
| `MP-CEBOLLA` | Cebolla | Materia Prima | GR |
| `MP-CHILE-DULCE` | Chile Dulce | Materia Prima | GR |
| `MP-BENZOATO` | Benzoato de Sodio | Materia Prima | GR |
| `PP-BLEND-CAHUITA` | Mezcla Cahuita (cruda) | Producto en Proceso | ML |
| `PT-CAHUITA-160` | Cahuita Salsa Caribeña 160ml | Producto Terminado | BOT160 |

---

### 5.3 Catálogo `productUnit`

**Leche de Coco** (base=ML, se compra en latas):

| `idUnit` | `factor` | `isBase` | `codeBarcode` | `namePresentation` | `brand` | `purchase` | `sale` |
|---|---|---|---|---|---|---|---|
| ML | 1.0 | ✓ | — | — | — | 0 | 0 |
| LATA400 | 400.0 | — | `8850999991234` | Coconut Milk 400ml | Aroy-D | 1 | 0 |
| LATA1000 | 1000.0 | — | `8850999991242` | Coconut Milk 1L | Aroy-D | 1 | 0 |

**Sal** (base=GR, a granel sin barcode):

| `idUnit` | `factor` | `isBase` | `codeBarcode` | `purchase` |
|---|---|---|---|---|
| GR | 1.0 | ✓ | — | 0 |
| KG | 1000.0 | — | — | 1 |
| SAC1K | 1000.0 | — | — | 1 |

**Chile Habanero, Ajo, Cebolla, Chile Dulce** (base=GR, a granel):

| `idUnit` | `factor` | `isBase` | `purchase` |
|---|---|---|---|
| GR | 1.0 | ✓ | 0 |
| KG | 1000.0 | — | 1 |

**Benzoato de Sodio** (base=GR, insumo químico — puede venir con barcode de proveedor):

| `idUnit` | `factor` | `isBase` | `codeBarcode` | `namePresentation` | `brand` | `purchase` |
|---|---|---|---|---|---|---|
| GR | 1.0 | ✓ | — | — | — | 0 |
| KG | 1000.0 | — | `7702009876543` | Benzoato de Sodio 1kg | QuimicaCR | 1 |

**Producto Terminado — Cahuita 160ml** (base=BOT160, barcode propio de la empresa):

| `idUnit` | `factor` | `isBase` | `codeBarcode` | `namePresentation` | `brand` | `purchase` | `sale` |
|---|---|---|---|---|---|---|---|
| BOT160 | 1.0 | ✓ | `7443036860195` | Cahuita Salsa Caribeña 160ml | Fiesta de Diablitos | 0 | 1 |
| CAJA12 | 12.0 | — | — | Caja Cahuita × 12 botellas | Fiesta de Diablitos | 0 | 1 |

> La unidad de **inventario** es `BOT160` (se cuentan botellas). Las unidades de **venta** son `BOT160` (minorista) y `CAJA12` (mayorista, factor = 12). El barcode `7443036860195` permite escanear la botella en punto de venta y auto-completar la línea de factura.

---

### 5.4 Recetas

**Receta 1 — Mezcla Cahuita**
Output: `PP-BLEND-CAHUITA`, `quantityOutput = 1000 ML`

| `idProductInput` | `quantityInput` (unidad base) |
|---|---|
| MP-LECHE-COCO | 600 ML |
| MP-CHILE-HABANERO | 80 GR |
| MP-SAL | 15 GR |
| MP-HIERBAS-FRESCAS | 30 GR |
| MP-CURRY | 10 GR |
| MP-AJO | 25 GR |
| MP-CEBOLLA | 50 GR |
| MP-CHILE-DULCE | 40 GR |
| MP-BENZOATO | 5 GR |

> La mezcla produce 1000 ML. El total de ingredientes suma menos de 1000 porque al licuar los sólidos se integran al volumen líquido. La merma de proceso se registra en `productionBatch` (V2).

**Receta 2 — Embotellado Cahuita 160ml**
Output: `PT-CAHUITA-160`, `quantityOutput = 6 BOT160`

| `idProductInput` | `quantityInput` |
|---|---|
| PP-BLEND-CAHUITA | 960 ML ← 6 × 160ml |

> ~40 ML de pérdida en proceso (boquillas, mangueras). La diferencia entre los 1000 ML fabricados en Receta 1 y los 960 ML usados en Receta 2 se captura como merma en `productionBatch`.

---

### 5.5 Compras — líneas de factura

**Latas de leche de coco Aroy-D 400ml (con escaneo de barcode):**
```
idProduct    = id("MP-LECHE-COCO")
idUnit       = LATA400           ← resuelto via codeBarcode "8850999991234"
quantity     = 30
quantityBase = 12000.000 ML      ← 30 × 400
unitPrice    = 1.25
```

**Sal a granel en sacos de 1kg:**
```
idProduct    = id("MP-SAL")
idUnit       = SAC1K
quantity     = 5
quantityBase = 5000.000 GR       ← 5 × 1000
unitPrice    = 0.80
```

**Chile habanero por kg:**
```
idProduct    = id("MP-CHILE-HABANERO")
idUnit       = KG
quantity     = 2
quantityBase = 2000.000 GR       ← 2 × 1000
unitPrice    = 3.50
```

**Benzoato de sodio con barcode de proveedor:**
```
idProduct    = id("MP-BENZOATO")
idUnit       = KG                ← resuelto via codeBarcode "7702009876543"
quantity     = 1
quantityBase = 1000.000 GR       ← 1 × 1000
unitPrice    = 4.20
```

---

### 5.6 Factura de venta — Cahuita (lote `26032002`)

Tras producir 6 BOT160 del lote `26032002` (vencimiento `19.03.27`):

**Venta minorista — 5 botellas individuales a tienda:**
```
salesInvoiceLine:
  idProduct    = id("PT-CAHUITA-160")
  idUnit       = BOT160          ← unidad base = unidad de venta
  quantity     = 5
  quantityBase = 5.000 BOT160    ← 5 × 1 = 5 botellas del inventario
  unitPrice    = 4.50
```

**Venta mayorista — 10 cajas de 12 unidades a supermercado:**
```
salesInvoiceLine:
  idProduct    = id("PT-CAHUITA-160")
  idUnit       = CAJA12          ← unidad de venta ≠ unidad base
  quantity     = 10
  quantityBase = 120.000 BOT160  ← 10 × 12 = 120 botellas se restan del inventario FEFO
  unitPrice    = 48.00           ← precio por caja
```

> El inventario solo conoce `BOT160`. La conversión `CAJA12 → BOT160` la hace el API usando `productUnit.conversionFactor = 12`. El lote `26032002` se decrementa en `quantityAvailable -= 120` si hay suficiente stock.

---

### 5.7 Vertical B — Reventa de una salsa de terceros

Si la empresa también distribuye una salsa que no fabrica:

```
product:
  codeProduct  = "RV-SALSA-CARIB-200"
  nameProduct  = "Salsa Caribeña La Abuela 200ml"
  productType  = Reventa
  idUnit       = BOT200             ← unidad base = botella

productUnit:
  BOT200 | factor=1.0 | isBase=1 | codeBarcode="7410000999001" |
           namePresentation="Salsa Caribeña La Abuela 200ml" |
           brandPresentation="La Abuela" | usedForPurchase=1 | usedForSale=1
```

Compra:
```
idProduct    = id("RV-SALSA-CARIB-200")
idUnit       = BOT200
quantity     = 48
quantityBase = 48.000
unitPrice    = 2.10
```

Sin duplicación de datos. Sin join tables. Sin ambigüedad contable.

---

## 6. Análisis: Facturas de Compra y Venta — ¿Separadas o Unificadas?

### 6.1 El dilema

¿Conviene mantener `purchaseInvoice` + `salesInvoice` como entidades separadas, o unificarlas en una sola tabla `invoice` con un `typeInvoice`?

### 6.2 Campos que comparten (≈60% de los campos)

`idFiscalPeriod`, `idCurrency`, `exchangeRateValue`, `dateInvoice`, `subTotalAmount`, `taxAmount`, `totalAmount`, `statusInvoice`, `descriptionInvoice`, `idBankAccount`.

### 6.3 Campos exclusivos por tipo

| Compra | Venta |
|---|---|
| `idContact` FK → `contact` (**proveedor**) | `idContact` FK → `contact` (**cliente**) |
| `numberInvoice` = número del proveedor (texto libre externo) | `numberInvoice` = consecutivo **autogenerado** por el sistema, regulado por autoridad fiscal |
| `idPurchaseInvoiceType` | `idSalesInvoiceType` |
| Documentos de importación (futuro) | Condición de crédito, dirección de entrega (futuro) |

> **Nota:** proveedores y clientes son la misma entidad `contact`. El rol queda implícito en el tipo de documento: un `contact` en `purchaseInvoice` actúa como proveedor; en `salesInvoice` actúa como cliente. Un mismo contacto puede ser ambos (compras y ventas con la misma empresa). El campo `providerName` (texto libre actual en la BD) se reemplaza por `idContact` FK NOT NULL en esta migración.

### 6.4 Tratamiento contable opuesto

| | Compra | Venta |
|---|---|---|
| Inventario / Gasto | **DR** (débito) | **CR** (crédito) |
| Proveedor / Cliente | **CR** | **DR** |
| IVA | Crédito fiscal (recuperable) | Débito fiscal (por pagar) |

Toda la lógica de generación de asientos cambia de sentido. Si se unifica, cada punto de la lógica contable necesita `if typeInvoice == 'Compra' then ... else ...`.

### 6.5 Problemas de unificar

| Problema | Detalle |
|---|---|
| **Nulos estructurales** | ~~`providerName` siempre NULL en ventas; `idContact` siempre NULL en compras~~. **Este nulo desaparece:** ambas tablas usan `idContact` FK → `contact`. El campo coincide; lo que difiere es el rol semántico (proveedor vs cliente), no la columna. Persisten nulos en campos como `idPurchaseInvoiceType` vs `idSalesInvoiceType`, documentos de importación, condición de crédito, etc. |
| **Número de factura** | Compra: libre (lo asigna el proveedor). Venta: consecutivo regulado autogenerado. La misma columna, semántica radicalmente opuesta. |
| **Lógica contable condicional** | Generación de asientos, cálculo de IVA, actualización de inventario: todo requiere preguntar el tipo. |
| **Extensibilidad** | Campos futuros de venta (crédito días, envío, exportación) acumulan nulos en la misma tabla. |
| **Reporting** | KPIs de compras y ventas son distintos. Queries más complejos con filtros de tipo. |

### 6.6 Decisión: entidades separadas ✅ APROBADO

**`purchaseInvoice` + `purchaseInvoiceLine`** — ya existen, se modifican según §4.10. `ProviderName` (texto libre) se reemplaza por `idContact` FK → `contact`.

**`salesInvoice` + `salesInvoiceLine`** — nuevas (V1.5, corto plazo). También usan `idContact` FK → `contact` (cliente).

Ambas tablas usan `idContact`; el rol (proveedor / cliente) queda implícito en el tipo de documento. Un mismo contacto puede aparecer en ambas.

La lógica compartida (validación de moneda, tipo de cambio, período fiscal, totales) se abstrae en **servicios del API**, no en la BD. La BD mantiene esquemas correctamente normalizados por entidad.

> **Analogía:** `bankMovement` y `accountingEntry` son entidades separadas aunque ambas "mueven dinero". La separación no duplica código — lo clarifica.

### 6.7 Schema `salesInvoice` — NUEVA (V1.5)

| Campo | Tipo | Descripción |
|---|---|---|
| `idSalesInvoice` | INT PK AI | |
| `idFiscalPeriod` | INT FK NOT NULL | FK → `fiscalPeriod` |
| `idCurrency` | INT FK NOT NULL | FK → `currency` |
| `idContact` | INT FK NOT NULL | FK → `contact` — cliente |
| `idSalesInvoiceType` | INT FK NOT NULL | FK → `salesInvoiceType` (contado, crédito, etc.) |
| `idBankAccount` | INT FK NULL | FK → `bankAccount` — si cobro en el momento |
| `numberInvoice` | VARCHAR(50) NOT NULL UNIQUE | Consecutivo autogenerado por el sistema (regulado fiscal) |
| `dateInvoice` | DATE NOT NULL | |
| `subTotalAmount` | DECIMAL(18,4) NOT NULL | |
| `taxAmount` | DECIMAL(18,4) NOT NULL | |
| `totalAmount` | DECIMAL(18,4) NOT NULL | |
| `statusInvoice` | VARCHAR(20) NOT NULL | `Borrador` \| `Confirmado` \| `Anulado` |
| `exchangeRateValue` | DECIMAL(18,6) NOT NULL | |
| `descriptionInvoice` | NVARCHAR(500) NULL | |
| `createdAt` | DATETIME2 NOT NULL DEFAULT GETUTCDATE() | |

### 6.8 Schema `salesInvoiceLine` — NUEVA (V1.5)

Misma lógica de `idUnit` + `quantityBase` que `purchaseInvoiceLine`. Al confirmar la factura, se decrementa `inventoryLot` siguiendo FEFO.

| Campo | Tipo | Descripción |
|---|---|---|
| `idSalesInvoiceLine` | INT PK AI | |
| `idSalesInvoice` | INT FK NOT NULL | |
| `idProduct` | INT FK NULL | FK → `product`. NULL para servicios que no mueven inventario. |
| `idUnit` | INT FK NULL | FK → `unitOfMeasure`. Debe existir en `productUnit` con `usedForSale = 1`. Ej: `BOT160`, `CAJA12`. |
| `descriptionLine` | NVARCHAR(500) NOT NULL | |
| `quantity` | DECIMAL(18,4) NOT NULL | Cantidad en la unidad de venta `idUnit`. |
| `quantityBase` | DECIMAL(18,6) NULL | `quantity × productUnit.conversionFactor` en unidad base. Se resta del inventario FEFO al confirmar. |
| `unitPrice` | DECIMAL(18,4) NOT NULL | Precio por unidad de `idUnit`. |
| `taxPercent` | DECIMAL(5,2) NOT NULL | |
| `totalLineAmount` | DECIMAL(18,4) NOT NULL | |

---

### 6.9 `salesInvoiceFiscal` — NUEVA (V1.5)

Tabla auxiliar 1:1 opcional con `salesInvoice`. Almacena los datos del documento tributario electrónico emitido ante Hacienda. Las facturas internas que no requieren timbrado fiscal no tienen fila en esta tabla (no acumulan nulos en `salesInvoice`).

| Campo | Tipo | Descripción |
|---|---|---|
| `idSalesInvoiceFiscal` | INT PK AI | |
| `idSalesInvoice` | INT FK NOT NULL UNIQUE | FK → `salesInvoice` |
| `numberFiscal` | VARCHAR(50) NOT NULL | Número de consecutivo asignado por Hacienda / DGII. |
| `urlXml` | NVARCHAR(500) NULL | URL del XML del documento electrónico. |
| `urlPdf` | NVARCHAR(500) NULL | URL del PDF del comprobante. |
| `statusFiscal` | VARCHAR(30) NOT NULL | `Pendiente` \| `Aceptado` \| `Rechazado` \| `Anulado` |
| `createdAt` | DATETIME2 NOT NULL DEFAULT GETUTCDATE() | |

> El consecutivo interno de `salesInvoice` (`numberInvoice` formato `YYYYMMDD-NNN`) se genera al confirmar y es independiente del `numberFiscal`. Primero se confirma la factura internamente; luego, si aplica, se emite el documento fiscal y se registra en esta tabla.

---

## 7. Tablas que NO cambian

| Tabla | Razón |
|---|---|
| `productCategory` | Clasificación de negocio (Salsas, Picantes, Artesanales…). Intacta. |
| `productProductCategory` | Join table M:N product ↔ category. Intacta. |
| `productAccount` | Distribución contable por producto. Intacta. La referencia directa desde `purchaseInvoiceLine.idProduct` resuelve la ambigüedad del §2.4. |

---

## 8. Resumen de Cambios por Tabla

| Tabla | Acción | Alcance | Detalle |
|---|---|---|---|
| `unitOfMeasure` | **NUEVA** | V1 | Catálogo global de unidades |
| `productType` | **NUEVA** | V1 | 4 tipos fijos sin CRUD: MP, PP, PT, Reventa |
| `product` | **MODIFICADA** | V1 | + `idProductType` FK, + `idUnit` FK (unidad base), + `idProductParent` FK auto-ref (variantes), + `averageCost` (costo promedio ponderado) |
| `productUnit` | **NUEVA** | V1 | Presentaciones por producto + conversión + barcode + marca. **Reemplaza `productSKU` y `productProductSKU`** |
| `productSKU` | **ELIMINADA** | — | Redundante con `productUnit` |
| `productProductSKU` | **ELIMINADA** | — | Join table ya innecesaria |
| `purchaseInvoice` | **MODIFICADA** | V1 | − `providerName` (texto libre), + `idContact` FK → `contact` (proveedor) |
| `purchaseInvoiceLine` | **MODIFICADA** | V1 | − `idProductSKU`, + `idProduct`, + `idUnit`, + `quantityBase`, + `lotNumber`, + `expirationDate` |
| `inventoryLot` | **NUEVA** | V1 | Stock por producto+lote, FEFO, trazabilidad de vencimiento. + `unitCost`, + `idInventoryAdjustment` FK. Solo modificable por documentos. |
| `inventoryAdjustment` | **NUEVA** | V1 | Cabecera de ajuste. Tipos: `Conteo Físico` \| `Producción` (V1 sin productionBatch) \| `Ajuste de Costo` |
| `inventoryAdjustmentLine` | **NUEVA** | V1 | Líneas con `quantityDelta` (puede ser 0) + `unitCostNew` opcional |
| `productRecipe` | **NUEVA** | V1 | Cabecera de receta de producción |
| `productRecipeLine` | **NUEVA** | V1 | Ingredientes de receta en unidades base |
| `salesInvoice` | **NUEVA** | V1.5 | Factura de venta. Consecutivo interno `YYYYMMDD-NNN`. FK `idContact` (cliente). |
| `salesInvoiceLine` | **NUEVA** | V1.5 | Líneas de venta con `idUnit` + `quantityBase` FEFO |
| `salesInvoiceFiscal` | **NUEVA** | V1.5 | Datos fiscales Hacienda (1:1 opcional). Evita nulos en `salesInvoice`. |
| `productCategory` | Sin cambios | — | — |
| `productProductCategory` | Sin cambios | — | — |
| `productAccount` | Sin cambios | — | — |

---

## 9. Deuda Técnica Fuera de Alcance (V2)

| Item | Descripción |
|---|---|
| `productionBatch` | Corrida automática de producción: fecha, número de lote generado, receta usada, cantidades reales vs planificadas, merma. En V1 la producción se registra manualmente ajustando `inventoryLot`. En V2 todo es automático. |
| `productionBatchInput` | Detalle de qué lotes de materias primas se consumieron en cada corrida: trazabilidad exacta hacia atrás. Permite responder "¿cuáles latas de leche de coco entraron al lote `26032002`?". |
| FEFO automático en producción | En V1 el FEFO lo aplica manualmente el operador al registrar el consumo. En V2 `productionBatch` aplica FEFO sobre `inventoryLot` automáticamente al ejecutar la corrida. |
| `salesInvoiceType` | Catálogo de tipos de venta (Contado, Crédito 30d, Exportación). Requerido por `salesInvoice`. Puede crearse en V1.5 junto con `salesInvoice`. |

---

## 10. Preguntas Abiertas — Resoluciones

1. **`productType` catálogo fijo** ✅ **APROBADO** — 4 valores fijos de sistema, sin CRUD expuesto al usuario.

2. **¿Un producto puede cambiar de tipo?** ✅ **APROBADO** — Puede cambiar de tipo **solo si no participa en ninguna receta** (ni como `idProductOutput` ni como `idProductInput`). Si tiene recetas activas o inactivas que lo referencian, el API rechaza el cambio con error 409.

3. **Unidad base con `usedForPurchase=0`** ✅ **APROBADO** — `usedForPurchase=0` en la unidad base (`ML`) significa que esa unidad no aparece como opción válida en líneas de factura de compra. El usuario selecciona presentaciones reales (LATA400, KG, SAC1K). La unidad base solo existe para inventario y recetas, no para documentos comerciales. `usedForSale=0` aplica el mismo criterio para ventas.

4. **Variantes del mismo producto** ✅ **APROBADO** — `PT-CAHUITA-160` y `PT-CAHUITA-500` son **dos `product` distintos** (inventario separado: unidades de 160ml por un lado, unidades de 500ml por otro). Cada uno tiene su propio catálogo `productUnit` (ej: `BOT160` + `CAJA12` para el de 160; `BOT500` + `CAJA100` para el de 500). Para agruparlos como familia se agrega `idProductParent` (FK auto-referencial opcional) en `product`. Ver actualización en §4.4.

5. **Migración de `purchaseInvoiceLine.idProductSKU`** ✅ **APROBADO** — Las facturas históricas se migran: se mapea `idProductSKU` → `idProduct` + `idUnit` usando la tabla `productProductSKU` existente. Los nombres de presentación se actualizan (pasan a `productUnit`).

6. **`quantityBase` almacenado en BD** ✅ **APROBADO** — Campo calculado y almacenado al confirmar la factura. No editable. Alimenta el inventario sin necesidad de recalcular en cada query.

7. **`lotNumber` — generación y unicidad** ✅ **APROBADO** — Para compras: `lotNumber` se genera automáticamente como `{idContact}-{numberInvoice}` (ej: `42-F-00123`). Garantiza unicidad por proveedor + número de factura. Si un mismo proveedor emite dos veces el mismo número (reutilización en años distintos), la segunda compra llega duplicada y el API la rechaza con error explicativo. Para ajustes: `lotNumber` sigue el mismo patrón `{idContact|SYSTEM}-{idAdjustmentDocument}`. El constraint `UQ(idProduct, lotNumber)` hace que dos líneas del mismo producto con el mismo lote consoliden en el mismo `inventoryLot` en lugar de crear dos filas.

8. **FEFO — obligatoriedad y costo** ✅ **APROBADO** — El lote de salida no es obligatorio pero el API sugiere uno por defecto (FEFO). El operador puede sobreescribirlo. Para **salida de inventario**: FEFO (primero el que vence antes). Para **costo del producto**: **costo promedio ponderado** — al ingresar cada lote se recalcula el costo promedio del producto ponderando el saldo existente + el nuevo ingreso. Implicación en §4.13: `inventoryLot` almacena `unitCost` y `product` almacena `averageCost` (se actualiza en cada confirmación de compra).

9. **Consecutivos de `salesInvoice`** ✅ **APROBADO** — Dos numeraciones independientes:
   - **Consecutivo interno:** generado al confirmar, formato `YYYYMMDD-NNN` (ej: `20260403-001`). Reinicia por día o es global según configuración.
   - **Datos fiscales Hacienda:** tabla auxiliar `salesInvoiceFiscal` en relación 1:1 opcional con `salesInvoice`. Contiene número fiscal, URL del XML y URL del PDF. Al ser tabla auxiliar, las facturas internas que no requieren documento fiscal no acumulan nulos en `salesInvoice`. Ver §6.9.

10. **`inventoryLot.quantityAvailable` — solo por documentos** ✅ **APROBADO** — `quantityAvailable` **no se modifica directamente nunca**. Solo tres tipos de documento pueden afectarlo:
    - `purchaseInvoice` confirmada → **suma** al lote (entrada).
    - `salesInvoice` confirmada → **resta** del lote (salida FEFO).
    - `inventoryAdjustment` confirmado → **suma, resta o cero** (ajuste físico, producción V1, o ajuste de costo).

    Tres tipos de ajuste:
    - `Conteo Físico` — corrige diferencias entre stock teórico y físico real.
    - `Producción` — reemplaza `productionBatch` en V1. Líneas negativas consumen materias primas y/o producto en proceso; líneas positivas crean el nuevo lote de producto en proceso o terminado en un único documento atómico. En V2 este tipo evoluciona a `productionBatch`.
    - `Ajuste de Costo` — `quantityDelta = 0`, solo modifica `unitCost` del lote y recalcula `product.averageCost`. Se usa para notas de crédito de proveedor o asignación de costos indirectos.

    Si el operador detecta diferencia en conteo físico, **debe crear un `inventoryAdjustment` tipo `Conteo Físico`**. No existe edición directa de saldo. Ver §4.14 y §4.15.

11. **Costo del lote creado en producción V1** ✅ **APROBADO** — El API calcula automáticamente `unitCostNew` para las líneas positivas sumando `|quantityDelta| × unitCost` de todas las líneas negativas del mismo ajuste y dividiendo entre las unidades base creadas. El valor es editable por el operador antes de confirmar (para incluir costos indirectos u otros ajustes). Ver fórmula en §4.15.

---

## 12. Preguntas Pendientes de Aprobación
