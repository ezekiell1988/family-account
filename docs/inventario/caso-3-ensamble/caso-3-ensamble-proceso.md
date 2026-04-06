# Caso 3 — Ensamble en venta (ejemplo: Hot dog)

**¿Qué es?** Produzco y vendo en el mismo flujo. Al confirmar el pedido con la bodega, el sistema genera automáticamente la orden de producción, consume los ingredientes, crea el lote del producto terminado y emite la factura confirmada.

> Ver configuraciones previas requeridas en [caso-3-ensamble-configuraciones.md](./caso-3-ensamble-configuraciones.md).

---

## Pasos

### 1. Autenticación

1. `POST /auth/request-pin` → el sistema envía un PIN al correo del usuario.
2. `POST /auth/login` → devuelve el `accessToken` (JWT).

---

### 2. Comprar los ingredientes

El proveedor entrega stock para preparar **50 hot dogs**. Se registra una factura con las cuatro líneas de ingredientes y se confirma.

1. `POST /purchase-invoices` — crea la factura de compra en borrador.
2. `POST /purchase-invoices/{id}/confirm` — confirma; el sistema crea un lote por cada ingrediente.

> Config.: [§4 ProductUnit](./caso-3-ensamble-configuraciones.md#4-unidades-de-compraventa-por-producto-productunit) · [§5 ProductAccount](./caso-3-ensamble-configuraciones.md#5-vínculo-producto--cuenta-contable-productaccount) · [§6 PurchaseInvoiceType](./caso-3-ensamble-configuraciones.md#6-tipo-de-factura-de-compra-purchaseinvoicetype)

---

### 3. Crear y confirmar el pedido (dispara todo el ciclo)

Un cliente pide **3 hot dogs**.

1. `POST /sales-orders` — crea el pedido en borrador con la línea del Hot Dog.
2. `POST /sales-orders/{id}/confirm` — confirma el pedido. El sistema ejecuta automáticamente en secuencia:

- Detecta que el Hot Dog tiene receta activa y usa la bodega por defecto.
- Crea la Orden de Producción (`Pendiente`) con línea Hot Dog × 3.
- Completa la OP: consume los ingredientes por FEFO (3 panes, 3 salchichas, 45 ml mostaza, 60 ml catsup), crea el lote **PT-HOT-DOG** y lo vincula al pedido.
- Marca el pedido como `Completado`.
- Genera la factura en borrador con `IdInventoryLot` del lote PT.
- Confirma la factura: descuenta el lote PT y genera los asientos de Ingresos y COGS.

> Config.: [§3 ProductRecipe](./caso-3-ensamble-configuraciones.md#3-receta-productrecipe--productrecipeline) · [§7 SalesInvoiceType](./caso-3-ensamble-configuraciones.md#7-tipo-de-factura-de-venta-salesinvoicetype)

---

### 4. Cancelar la venta (devolución total)

Si se anula la factura, el sistema devuelve las 3 unidades al lote PT y revierte los asientos contables.

1. `POST /sales-invoices/{id}/cancel`

---

### 5. Devolución parcial

El cliente devuelve **1 hot dog** de los 3 facturados. Como la factura tiene el lote PT vinculado, la devolución funciona igual que en Manufactura.

1. `POST /sales-invoices/{id}/partial-return` — body: `{ "lines": [{ "idSalesInvoiceLine": N, "quantity": 1 }] }`

El sistema reintegra 1 unidad al lote PT y genera los asientos de reversión de COGS e ingresos.

---

### 6. Regalía (solo administrador)

El administrador regala **2 hot dogs** al personal. No se genera factura; se registra un ajuste de salida sobre el lote PT.

1. `POST /inventory-adjustments` — línea con `idInventoryLot` del lote PT, `quantityDelta = -2`.
2. `POST /inventory-adjustments/{id}/confirm` — descuenta 2 unidades del lote y genera el asiento de merma.

> Config.: [§8 InventoryAdjustmentType](./caso-3-ensamble-configuraciones.md#8-tipo-de-ajuste-de-inventario-inventoryadjustmenttype)

---

## Verificación final de stock

- `GET /inventory-lots/by-product/11` — consulta el stock del lote PT-HOT-DOG.

**Fórmula:** 3 (producidos) − 3 (venta) + 1 (devolución) − 2 (regalía) = **−1** *(si se dan los pasos en este orden exacto)*

> En producción real los pasos de regalía y devolución son independientes. Para 50 hot dogs comprados y vendidos gradualmente, consultar también los lotes de ingredientes con `GET /inventory-lots/by-product/{idIngrediente}`.

