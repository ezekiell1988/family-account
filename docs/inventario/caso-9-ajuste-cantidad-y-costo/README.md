# Caso 9 — Ajuste de cantidad y costo (ejemplo: Factura con cantidad y precio incorrectos)

**¿Qué es?** Se descubre que una compra se registró con el precio equivocado **y** con una cantidad diferente a la que realmente entró a bodega. Hay que corregir ambas cosas.

## Pasos

1. El operador detecta el error revisando la factura del proveedor contra el registro del sistema.
2. Registra el conteo físico real del producto afectado.
3. El sistema genera un **ajuste de cantidad** para corregir el stock (puede ser aumento o reducción).
4. El operador registra el precio correcto de la compra.
5. El sistema genera un **ajuste de costo** para corregir el valor del lote.
6. Ambos ajustes quedan registrados con fecha, responsable y motivo.
7. A partir de ese momento el inventario refleja las cantidades y el costo reales.

## Ejemplo concreto

Se compró aceite y se registró así:
- **Cantidad registrada**: 100 litros a **$1.20/lt**
- **Realidad**: llegaron solo **90 litros** y el precio correcto era **$1.50/lt**

El operador aplica los dos ajustes:
- Ajuste de cantidad: −10 litros → stock queda en 90 litros.
- Ajuste de costo: de $1.20 a $1.50/lt → el lote queda correctamente valorizado.

---

## Consideraciones

- El ajuste de cantidad **sí modifica el stock disponible**.
- El ajuste de costo **no modifica el stock**, solo el valor del inventario.
- Ambos ajustes pueden hacerse en una sola operación o por separado; en cualquier caso quedan vinculados al mismo lote.
- El historial del lote muestra los dos movimientos con su motivo para auditoría.
