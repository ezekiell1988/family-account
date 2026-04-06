# Caso 3 — Ensamble en venta (ejemplo: Hot dog)

**¿Qué es?** No produzco con anticipación. Los ingredientes se descuentan en el mismo momento en que confirmo la venta.

## Pasos

1. Tengo en bodega: **panes, salchichas, mostaza, catsup**.
2. El cliente pide **3 hot dogs**.
3. Agrego "Hot dog" a la factura.
4. Al confirmar la factura, el sistema busca automáticamente la receta del hot dog.
5. El sistema descuenta por cada hot dog: 1 pan, 1 salchicha, una porción de mostaza y catsup.
6. No necesito indicarle nada al sistema — él solo sabe qué descontar.

**Si cancelo la factura:** todos los ingredientes regresan a bodega automáticamente.

**Devolución parcial:** si el cliente devuelve parte de los hot dogs facturados (por ejemplo, 1 de 3), el operador registra la devolución indicando la cantidad. El sistema recalcula los ingredientes de esa unidad usando la receta y los devuelve a bodega. Se genera una nota de crédito por el monto correspondiente.

---

## Documento de regalía (solo administrador)

Un usuario administrador puede emitir un **documento de regalía** sobre el producto ensamblado. Este documento:
- **No genera venta ni factura** al cliente.
- **Descuenta los ingredientes** de bodega usando la receta, como un ajuste de cantidad.
- Queda registrado con motivo y responsable para auditoría.

**Ejemplo:** el administrador regala 2 hot dogs al personal → el sistema descuenta los ingredientes correspondientes (2 panes, 2 salchichas, etc.) y registra el ajuste como regalía.

## Diferencia con Manufactura

- En manufactura produzco primero y luego vendo el producto terminado.
- En ensamble no hay producción previa; los ingredientes salen directo al vender.
