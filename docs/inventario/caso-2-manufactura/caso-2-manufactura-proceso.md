# Caso 2 — Manufactura (ejemplo: Chile embotellado)

**¿Qué es?** Compro materias primas, las transformo en un producto terminado mediante una orden de producción y luego lo vendo.

> Ver configuraciones previas requeridas en [caso-2-manufactura-configuraciones.md](./caso-2-manufactura-configuraciones.md).

---

## Pasos

### 1. Autenticación

El sistema usa autenticación en dos pasos:

1. `POST /auth/request-pin` → el sistema envía un PIN al correo del usuario.
2. `POST /auth/login` → devuelve el `accessToken` (JWT) para usar en los siguientes pasos.

---

### 2. Comprar las materias primas

El proveedor entrega los ingredientes para producir **100 frascos**. Se registra una factura con las cuatro líneas de MP y se confirma.

1. `POST /purchase-invoices` — crea la factura de compra en borrador.
2. `POST /purchase-invoices/{id}/confirm` — confirma la factura; el sistema crea un lote por cada línea de MP disponible en bodega.

> El costo se acredita a **1.1.07.02 Materias Primas** (cuenta 110) si se configuraron `ProductAccount` para los MP, o a la cuenta de inventario por defecto (109) si no. Ver [§5 ProductAccount](./caso-2-manufactura-configuraciones.md#5-vínculo-producto--cuenta-contable-productaccount) y [§6 PurchaseInvoiceType](./caso-2-manufactura-configuraciones.md#6-tipo-de-factura-de-compra-purchaseinvoicetype).

> Config.: [§4 ProductUnit](./caso-2-manufactura-configuraciones.md#4-unidades-de-compraventa-por-producto-productunit) · [§11 Período fiscal](./caso-2-manufactura-configuraciones.md#11-período-fiscal-y-almacén)

---

### 3. Crear la orden de producción

Se abre una orden para producir **100 frascos** de Chile Embotellado.

1. `POST /production-orders` — crea la orden en estado **Borrador**.

> La orden queda con `NumberProductionOrder = "BORRADOR"` hasta confirmarla. El sistema busca la receta activa del producto al momento de completar (no al crear). Ver [§3 ProductRecipe](./caso-2-manufactura-configuraciones.md#3-receta-productrecipe--productrecipeline) · [§9 Estados de la orden](./caso-2-manufactura-configuraciones.md#9-orden-de-producción--estados).

---

### 4. Avanzar la orden a Pendiente

El sistema asigna el número correlativo (`OP-2026-NNNN`).

1. `PATCH /production-orders/{id}/status` — body: `{ "statusProductionOrder": "Pendiente" }`

---

### 5. Iniciar producción

1. `PATCH /production-orders/{id}/status` — body: `{ "statusProductionOrder": "EnProceso" }`

---

### 6. Completar la producción

Al completar, el sistema consume los lotes de MP por **FEFO**, genera los asientos de producción y crea el lote del producto terminado.

1. `PATCH /production-orders/{id}/status` — body: `{ "statusProductionOrder": "Completado" }`

> Si hay stock insuficiente de algún insumo la orden completa de todas formas y devuelve un array `warnings`. Se puede informar una cantidad real diferente a la requerida enviando `lines[].quantityProduced` en el body.

> Config.: [§7 InventoryAdjustmentType PRODUCCION](./caso-2-manufactura-configuraciones.md#7-tipo-de-ajuste-de-inventario-inventoryadjustmenttype)

---

### 7. Registrar la venta del producto terminado

Un cliente pide **30 frascos**. Se consultan los lotes disponibles del PT, se crea la factura y se confirma.

1. `GET /inventory-lots/by-product/6` — obtiene los lotes disponibles con su stock y costo unitario.
2. `POST /sales-invoices` — crea la factura en borrador indicando el lote a descontar.
3. `POST /sales-invoices/{id}/confirm` — confirma la factura; el sistema descuenta 30 frascos del lote (quedan 70) y genera el asiento de COGS automáticamente.

> Config.: [§8 SalesInvoiceType](./caso-2-manufactura-configuraciones.md#8-tipo-de-factura-de-venta-salesinvoicetype)

---

### 8. Devolución parcial

El cliente devuelve **5 frascos** en buen estado.

1. `POST /sales-invoices/{id}/partial-return` — reintegra 5 unidades al lote de PT (quedan 75) y genera los asientos de reversión de COGS e ingresos automáticamente.

---

### 9. Regalía / ajuste de inventario

Un administrador regala **2 frascos** a un distribuidor como muestra. Se registra un ajuste de inventario con cantidad negativa y se confirma. El sistema descuenta las 2 unidades del lote (quedan 73) y genera el asiento de merma.

1. `POST /inventory-adjustments` — crea el ajuste en borrador.
2. `POST /inventory-adjustments/{id}/confirm` — confirma el ajuste.

> Config.: [§7 InventoryAdjustmentType](./caso-2-manufactura-configuraciones.md#7-tipo-de-ajuste-de-inventario-inventoryadjustmenttype)

---

## Verificación final de stock

- `GET /inventory-lots/stock/6`

**Fórmula:** 100 (producidos) − 30 (venta) + 5 (devolución) − 2 (regalía) = **73 unidades**

**Si se anula la orden de producción** (antes de Completado): los lotes de MP no se ven afectados; solo cambia el estado de la orden a `Anulado`.

**Si se cancela la venta:** las 30 unidades vuelven al lote de PT automáticamente.
