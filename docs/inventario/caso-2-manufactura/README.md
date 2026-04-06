# Caso 2 — Manufactura (ejemplo: Chile embotellado)

**¿Qué es?** Compro materias primas, las transformo en un producto terminado y luego vendo ese producto.

## Pasos

1. Compro los insumos: **chiles, vinagre, sal, frascos**.
2. El sistema registra cada insumo en bodega como disponible.
3. Abro una **Orden de Producción** para hacer "Chile embotellado marca X".
4. La orden ya sabe qué ingredientes necesita y en qué cantidad (eso está en la receta).
5. Cuando termino de producir, marco la orden como **Completada**.
6. El sistema descuenta los insumos usados de bodega (auto, de los lotes más antiguos primero).
7. El sistema crea un lote nuevo de "Chile embotellado" con las unidades producidas.
8. Cuando llega un cliente, vendo desde ese lote de producto terminado.

## Ejemplo concreto

- Receta: 1 frasco de chile necesita 200g de chile, 50ml de vinagre, 5g de sal.
- Produzco 100 frascos → el sistema descuenta 20 kg de chile, 5 lt de vinagre y 500g de sal.
- Quedan 100 frascos disponibles para vender.

---

## Anulación y devolución

**Si cancelo la factura de venta:** los frascos vendidos vuelven al lote de producto terminado automáticamente.

**Devolución parcial:** si el cliente devuelve solo una parte (por ejemplo, 5 de 20 frascos facturados), el operador registra la devolución indicando cantidad y lote. El sistema suma esos 5 frascos de vuelta al inventario de producto terminado y genera una nota de crédito.

---

## Documento de regalía (solo administrador)

Un usuario administrador puede emitir un **documento de regalía** sobre el producto terminado. Este documento:
- **No genera venta ni factura** al cliente.
- **Descuenta el inventario** como un ajuste de cantidad.
- Queda registrado con motivo y responsable para auditoría.

**Ejemplo:** el administrador regala 10 frascos de chile embotellado a un distribuidor → el sistema descuenta 10 frascos del lote indicado y registra el ajuste como regalía.
