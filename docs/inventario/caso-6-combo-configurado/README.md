# Caso 6 — Combo configurado (ejemplo: 2 Pizzas + Bebida)

**¿Qué es?** El cliente compra un combo que tiene varios "espacios" (slots). En cada espacio elige un producto y sus opciones. El negocio puede fijar algunas opciones de antemano (por ejemplo: todas las pizzas del combo son Grandes).

## Pasos

1. El cliente pide el combo **"2 Pizzas + Bebida"** (precio base $25.00).
2. El sistema le muestra los 3 espacios del combo:
   - **Espacio 1 — Pizza #1**: Tamaño ya fijado en Grande. El cliente elige masa, sabor y extras.
   - **Espacio 2 — Pizza #2**: Igual, Tamaño ya fijado en Grande. El cliente elige masa, sabor y extras.
   - **Espacio 3 — Bebida**: El cliente elige entre Coca-Cola, Sprite o agua.
3. Cliente elige:
   - Pizza #1: Masa Delgada, Pepperoni, Doble Queso (+$1.50)
   - Pizza #2: Masa Clásica, Hawaiian
   - Bebida: Coca-Cola
4. El sistema calcula el precio final: $25.00 base + $1.50 del Doble Queso = **$26.50 total**.
5. El operador confirma el pedido.
6. Se envían **2 Órdenes de Producción** automáticamente, una por cada pizza.
   - OP #1: ingredientes de Pizza Grande + Masa Delgada + Pepperoni + Doble Queso.
   - OP #2: ingredientes de Pizza Grande + Masa Clásica + Hawaiian.
7. La bebida no va a producción — se sirve directo de bodega.
8. Cuando ambas órdenes quedan **Completadas**, el sistema descuenta todos los ingredientes usados.
9. El operador marca el pedido como entregado.
10. Se genera la factura mostrando:
    - Combo 2 Pizzas + Bebida ........... $25.00
    - → Pizza #1: Grande, Delgada, Pepperoni
    - → → Extra: Doble Queso ............. $1.50
    - → Pizza #2: Grande, Clásica, Hawaiian
    - → Bebida: Coca-Cola
    - **Total: $26.50**

---

## Anulación y devolución

**Si cancelo la factura del combo:** los lotes de producto terminado de cada pizza vuelven al inventario. La bebida (sirve directo de bodega) también regresa. Los ingredientes descontados durante la producción **no** se revierten (la producción ya ocurrió).

**Devolución parcial:** si el cliente devuelve solo parte del combo (por ejemplo, devuelve una pizza pero conserva la otra y la bebida), el operador registra la devolución indicando qué ítems del combo retorna. El sistema ajusta el inventario solo de los ítems devueltos y genera una nota de crédito proporcional.

---

## Documento de regalía (solo administrador)

Un usuario administrador puede emitir un **documento de regalía** sobre cualquier ítem del combo o el combo completo. Este documento:
- **No genera venta ni factura** al cliente.
- **Descuenta el inventario** de los productos e ingredientes involucrados como un ajuste de cantidad.
- Queda registrado con motivo y responsable para auditoría.

**Ejemplo:** el administrador regala un combo completo a un cliente en compensación → el sistema descuenta los lotes de las 2 pizzas y la bebida, y registra el ajuste como regalía.
