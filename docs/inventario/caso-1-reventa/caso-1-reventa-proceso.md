# Caso 1 — Reventa (ejemplo: Coca-Cola)

**¿Qué es?** Compro algo, lo guardo en bodega y luego lo vendo con ganancia.

> Ver configuraciones previas requeridas en [caso-1-reventa-configuraciones.md](./caso-1-reventa-configuraciones.md).

---

## Pasos

### 1. Autenticación

El sistema usa autenticación en dos pasos:

1. `POST /auth/request-pin` → el sistema envía un PIN al correo del usuario.
2. `POST /auth/login` → devuelve el `accessToken` (JWT) para usar en los siguientes pasos.

---

### 2. Registrar la compra

El proveedor entrega **100 cajas de Coca-Cola**. Se registra la factura de compra y se confirma.

1. `POST /purchase-invoices` — crea la factura en borrador.
2. `POST /purchase-invoices/{id}/confirm` — confirma la factura; el sistema crea el lote de inventario con las 100 unidades disponibles.

> El costo se acredita a la cuenta de inventario por defecto (configurada en el tipo de factura). Si se quiere cargar a una cuenta de gasto en su lugar, se debe crear un `ProductAccount` explícito para ese producto (`POST /product-accounts`) antes de confirmar.

> Config.: [§2 ProductUnit](./caso-1-reventa-configuraciones.md#2-unidad-de-medida-y-presentación-productunit) · [§3 PurchaseInvoiceType](./caso-1-reventa-configuraciones.md#3-tipo-de-factura-de-compra-purchaseinvoicetype) · [§5 ProductAccount](./caso-1-reventa-configuraciones.md#5-productaccount--vínculo-producto--cuenta-contable) · [§9 Período fiscal](./caso-1-reventa-configuraciones.md#9-período-fiscal) · [§10 Almacén](./caso-1-reventa-configuraciones.md#10-almacén-warehouse)

---

### 3. Registrar la venta

Un cliente pide **10 cajas**. Se consultan los lotes disponibles, se elige el lote de la compra anterior, se crea la factura y se confirma.

1. `GET /inventory-lots/by-product/{idProduct}` — obtiene los lotes disponibles con su stock.
2. `POST /sales-invoices` — crea la factura en borrador indicando el lote a descontar.
3. `POST /sales-invoices/{id}/confirm` — confirma la factura; el sistema descuenta 10 cajas del lote (quedan 90) y genera el asiento de COGS automáticamente.

> Config.: [§4 SalesInvoiceType](./caso-1-reventa-configuraciones.md#4-tipo-de-factura-de-venta-salesinvoicetype)

---

### 4. Devolución parcial

El cliente devuelve **3 cajas** dañadas en tránsito. Se registra la devolución indicando el modo de reintegro (`EfectivoInmediato` o `NotaCredito`).

1. `POST /sales-invoices/{id}/partial-return` — reintegra las 3 cajas al lote (quedan 93) y genera los asientos de reversión de COGS e ingresos automáticamente.

---

### 5. Ajuste de inventario / Regalía

Un administrador regala **2 cajas** a un cliente VIP. Se registra un ajuste de inventario con cantidad negativa y se confirma. El sistema descuenta las 2 cajas del lote (quedan 91) y genera el asiento de merma.

1. `POST /inventory-adjustments` — crea el ajuste en borrador.
2. `POST /inventory-adjustments/{id}/confirm` — confirma el ajuste.

> Config.: [§6 InventoryAdjustmentType](./caso-1-reventa-configuraciones.md#6-tipo-de-ajuste-de-inventario-inventoryadjustmenttype)

---

## Verificación final de stock

- `GET /inventory-lots/stock/{idProduct}`

**Fórmula:** 100 (compra) − 10 (venta) + 3 (devolución) − 2 (regalía) = **91 unidades**

**Si se cancela la venta:** las 10 cajas vuelven al lote automáticamente.

**Si se anula la factura de compra:** los lotes creados al confirmarla se reducen automáticamente. Si alguno ya fue consumido parcialmente por una venta, la anulación es bloqueada hasta que se anulen primero las facturas de venta que dependen de ese lote.
