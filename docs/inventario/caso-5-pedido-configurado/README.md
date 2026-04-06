# Caso 5 — Pedido configurado (ejemplo: Pizza)

**¿Qué es?** El cliente arma su pedido eligiendo opciones (tamaño, masa, sabor, extras). El pedido pasa a producción y luego se entrega y factura.

## Pasos

1. El cliente hace un **pedido de 1 pizza**.
2. Elige las opciones: **Grande, Masa Delgada, Pepperoni, Extra Queso**.
3. El sistema guarda el pedido con todas esas opciones.
4. El operador confirma el pedido.
5. Se envía a producción → el sistema crea una Orden de Producción automáticamente.
6. La orden calcula todos los ingredientes necesarios sumando: la receta base de la pizza + los ingredientes de cada opción elegida.
   - Ejemplo: Grande agrega más harina y agua. Extra Queso agrega mozzarella.
7. Los cocineros trabajan la orden.
8. Al marcar la orden como **Completada**, el sistema descuenta los ingredientes de bodega.
9. Se crea un lote de "Pizza terminada".
10. El operador marca el pedido como entregado.
11. Se genera la factura al cliente mostrando: Pizza + opciones elegidas + precio total.

---

## Anulación y devolución

**Si cancelo la factura:** el lote de producto terminado (pizza) vuelve al inventario. Los ingredientes que ya fueron descontados al completar la producción **no** se revierten automáticamente en este punto (la producción ya ocurrió).

**Devolución parcial:** si el pedido incluía varias unidades y el cliente devuelve solo algunas, el operador registra la devolución indicando la cantidad. El sistema suma esas unidades de vuelta al inventario de producto terminado y genera una nota de crédito.

---

## Documento de regalía (solo administrador)

Un usuario administrador puede emitir un **documento de regalía** sobre el producto terminado. Este documento:
- **No genera venta ni factura** al cliente.
- **Descuenta el inventario** como un ajuste de cantidad.
- Queda registrado con motivo y responsable para auditoría.

**Ejemplo:** el administrador regala una pizza al repartidor → el sistema descuenta 1 unidad del lote de pizza terminada y registra el ajuste como regalía.
