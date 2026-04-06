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

---

### 3. Registrar la venta

Un cliente pide **10 cajas**. Se consultan los lotes disponibles, se elige el lote de la compra anterior, se crea la factura y se confirma.

1. `GET /inventory-lots/by-product/{idProduct}` — obtiene los lotes disponibles con su stock.
2. `POST /sales-invoices` — crea la factura en borrador indicando el lote a descontar.
3. `POST /sales-invoices/{id}/confirm` — confirma la factura; el sistema descuenta 10 cajas del lote (quedan 90) y genera el asiento de COGS automáticamente.

---

### 4. Devolución parcial

El cliente devuelve **3 cajas** dañadas en tránsito. El sistema las reintegra al lote (quedan 93) y genera la reversa de COGS.

- `POST /sales-invoices/{id}/partial-return`

> La cantidad a devolver por líte no puede superar la cantidad originalmente vendida en esa línea. El sistema responde HTTP 422 si se intenta devolver un exceso.

---

### 5. Reintegro bancario al cliente (manual)

La devolución del efectivo al cliente se registra como un asiento manual.

- `POST /accounting-entries`

---

### 6. Ajuste de inventario / Regalía

Un administrador regala **2 cajas** a un cliente VIP. Se registra un ajuste de inventario con cantidad negativa y se confirma. El sistema descuenta las 2 cajas del lote (quedan 91) y genera el asiento de merma.

1. `POST /inventory-adjustments` — crea el ajuste en borrador.
2. `POST /inventory-adjustments/{id}/confirm` — confirma el ajuste.

---

## Verificación final de stock

- `GET /inventory-lots/stock/{idProduct}`

**Fórmula:** 100 (compra) − 10 (venta) + 3 (devolución) − 2 (regalía) = **91 unidades**

**Si se cancela la venta:** las 10 cajas vuelven al lote automáticamente.

**Si se anula la factura de compra:** los lotes creados al confirmarla se reducen automáticamente. Si alguno ya fue consumido parcialmente por una venta, la anulación es bloqueada hasta que se anulen primero las facturas de venta que dependen de ese lote.
