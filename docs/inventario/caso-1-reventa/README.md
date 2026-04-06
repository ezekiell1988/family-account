# Caso 1 — Reventa (ejemplo: Coca-Cola)

**¿Qué es?** Compro algo, lo guardo en bodega y luego lo vendo con ganancia.

## Pasos

1. El proveedor me entrega **100 cajas de Coca-Cola**.
2. Registro la compra en el sistema → el sistema anota que tengo 100 cajas disponibles en bodega.
3. Un cliente me pide **10 cajas**.
4. Al confirmar la venta, elijo de qué lote de compra salen esas 10 cajas.
5. El sistema descuenta 10 cajas → quedan 90 disponibles.
6. El sistema calcula automáticamente cuánto me costó lo que vendí.

**Si cancelo la venta:** las 10 cajas vuelven a la bodega automáticamente.

**Devolución parcial:** si el cliente devuelve solo una parte (por ejemplo, 3 de las 10 cajas), el operador registra una devolución parcial indicando la cantidad y el lote de origen. El sistema suma esas 3 cajas de vuelta al inventario y genera una nota de crédito por el monto correspondiente.

---

## Documento de regalía (solo administrador)

Un usuario administrador puede emitir un **documento de regalía** sobre cualquier producto. Este documento:
- **No genera venta ni factura** al cliente.
- **Descuenta el inventario** como un ajuste de cantidad.
- Queda registrado con motivo y responsable para auditoría.

**Ejemplo:** el administrador regala 2 cajas de Coca-Cola a un cliente VIP → el sistema descuenta 2 cajas del lote indicado y registra el ajuste como regalía.
