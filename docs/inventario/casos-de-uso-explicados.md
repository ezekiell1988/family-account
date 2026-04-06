# Los 9 casos de uso del inventario — Guía para todos

> Este documento explica de forma sencilla cómo funciona el inventario en cada situación del negocio.
> No se necesita conocimiento técnico. Solo leer y seguir los pasos.

---

## Caso 1 — Reventa (ejemplo: Coca-Cola)

**¿Qué es?** Compro algo, lo guardo en bodega y luego lo vendo con ganancia.

**Pasos:**

1. El proveedor me entrega **100 cajas de Coca-Cola**.
2. Registro la compra en el sistema → el sistema anota que tengo 100 cajas disponibles en bodega.
3. Un cliente me pide **10 cajas**.
4. Al confirmar la venta, elijo de qué lote de compra salen esas 10 cajas.
5. El sistema descuenta 10 cajas → quedan 90 disponibles.
6. El sistema calcula automáticamente cuánto me costó lo que vendí.

**Si cancelo la venta:** las 10 cajas vuelven a la bodega automáticamente.

**Devolución parcial:** si el cliente devuelve solo una parte (por ejemplo, 3 de las 10 cajas), el operador registra la devolución indicando cantidad y lote. El sistema suma esas 3 cajas de vuelta al inventario y genera una nota de crédito.

**Regalía (solo administrador):** un administrador puede emitir un documento de regalía que descuenta inventario sin generar venta ni factura. Queda registrado con motivo y responsable.

---

## Caso 2 — Manufactura (ejemplo: Chile embotellado)

**¿Qué es?** Compro materias primas, las transformo en un producto terminado y luego vendo ese producto.

**Pasos:**

1. Compro los insumos: **chiles, vinagre, sal, frascos**.
2. El sistema registra cada insumo en bodega como disponible.
3. Abro una **Orden de Producción** para hacer "Chile embotellado marca X".
4. La orden ya sabe qué ingredientes necesita y en qué cantidad (eso está en la receta).
5. Cuando termino de producir, marco la orden como **Completada**.
6. El sistema descuenta los insumos usados de bodega (auto, de los lotes más antiguos primero).
7. El sistema crea un lote nuevo de "Chile embotellado" con las unidades producidas.
8. Cuando llega un cliente, vendo desde ese lote de producto terminado.

**Ejemplo concreto:**
- Receta: 1 frasco de chile necesita 200g de chile, 50ml de vinagre, 5g de sal.
- Produzco 100 frascos → el sistema descuenta 20 kg de chile, 5 lt de vinagre y 500g de sal.
- Quedan 100 frascos disponibles para vender.

**Si cancelo la factura de venta:** los frascos vendidos vuelven al lote de producto terminado automáticamente.

**Devolución parcial:** si el cliente devuelve solo parte de los frascos facturados, el operador registra la cantidad devuelta. El sistema suma esos frascos al inventario de producto terminado y genera una nota de crédito.

**Regalía (solo administrador):** un administrador puede emitir un documento de regalía sobre el producto terminado que descuenta inventario sin generar venta ni factura. Queda registrado con motivo y responsable.

---

## Caso 3 — Ensamble en venta (ejemplo: Hot dog)

**¿Qué es?** No produzco con anticipación. Los ingredientes se descuentan en el mismo momento en que confirmo la venta.

**Pasos:**

1. Tengo en bodega: **panes, salchichas, mostaza, catsup**.
2. El cliente pide **3 hot dogs**.
3. Agrego "Hot dog" a la factura.
4. Al confirmar la factura, el sistema busca automáticamente la receta del hot dog.
5. El sistema descuenta por cada hot dog: 1 pan, 1 salchicha, una porción de mostaza y catsup.
6. No necesito indicarle nada al sistema — él solo sabe qué descontar.

**Diferencia con Manufactura:**
- En manufactura produzco primero y luego vendo el producto terminado.
- En ensamble no hay producción previa; los ingredientes salen directo al vender.

**Si cancelo la factura:** todos los ingredientes regresan a bodega automáticamente.

**Devolución parcial:** si el cliente devuelve parte de los hot dogs facturados, el operador registra la cantidad. El sistema recalcula los ingredientes de esas unidades usando la receta y los devuelve a bodega. Se genera una nota de crédito.

**Regalía (solo administrador):** un administrador puede emitir un documento de regalía sobre el producto ensamblado. El sistema descuenta los ingredientes correspondientes usando la receta, sin generar venta ni factura. Queda registrado con motivo y responsable.

---

## Caso 4 — Variantes (ejemplo: Ropa por talla y color)

**¿Qué es?** Vendo un mismo producto en varias versiones (talla S, M, L / color azul, rojo). Cada versión tiene su propio inventario separado.

**Pasos:**

1. En el catálogo creo el producto padre: **"Camisa Oxford"**.
2. Defino sus variantes: Talla S Azul, Talla M Azul, Talla L Azul, Talla S Rojo, etc.
3. Compro **20 camisas Talla M Azul** → solo ese inventario sube.
4. El cliente quiere una **Talla L Rojo** → si no hay stock de esa variante exacta, el sistema avisa.
5. Al facturar, el operador elige la variante exacta (Talla M Azul) y el sistema descuenta de ese inventario.

**Ejemplo concreto:**
- Talla S Azul: 5 unidades disponibles.
- Talla M Azul: 20 unidades disponibles.
- Talla L Rojo: 0 unidades → no se puede vender.

**Si cancelo la factura:** las unidades de la variante vendida vuelven a su inventario específico automáticamente.

**Devolución parcial:** si el cliente devuelve solo algunas unidades de la variante, el operador registra variante y cantidad. El sistema suma esas unidades al inventario de esa variante exacta y genera una nota de crédito.

**Regalía (solo administrador):** un administrador puede emitir un documento de regalía sobre una variante específica que descuenta inventario sin generar venta ni factura. Queda registrado con motivo y responsable.

---

## Caso 5 — Pedido configurado (ejemplo: Pizza)

**¿Qué es?** El cliente arma su pedido eligiendo opciones (tamaño, masa, sabor, extras). El pedido pasa a producción y luego se entrega y factura.

**Pasos:**

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

**Si cancelo la factura:** el lote de pizza terminada vuelve al inventario. Los ingredientes descontados al completar la producción no se revierten (la producción ya ocurrió).

**Devolución parcial:** si el pedido incluía varias unidades y el cliente devuelve solo algunas, el operador registra la cantidad. El sistema devuelve esas unidades al inventario de producto terminado y genera una nota de crédito.

**Regalía (solo administrador):** un administrador puede emitir un documento de regalía sobre el producto terminado que descuenta inventario sin generar venta ni factura. Queda registrado con motivo y responsable.

---

## Caso 6 — Combo configurado (ejemplo: 2 Pizzas + Bebida)

**¿Qué es?** El cliente compra un combo que tiene varios "espacios" (slots). En cada espacio elige un producto y sus opciones. El negocio puede fijar algunas opciones de antemano (por ejemplo: todas las pizzas del combo son Grandes).

**Pasos:**

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

**Si cancelo la factura:** los lotes de pizza terminada y la bebida vuelven al inventario. Los ingredientes descontados durante la producción no se revierten.

**Devolución parcial:** si el cliente devuelve solo parte del combo (por ejemplo, una pizza), el operador registra qué ítems retorna. El sistema ajusta el inventario de esos ítems y genera una nota de crédito proporcional.

**Regalía (solo administrador):** un administrador puede emitir un documento de regalía sobre cualquier ítem del combo o el combo completo, descontando inventario sin generar venta ni factura. Queda registrado con motivo y responsable.

---

## Casos 7, 8 y 9 — Ajustes de inventario

Antes de vender, y en cualquier momento de la operación, pueden ocurrir ajustes de **cantidad**, de **costo**, o de ambos a la vez.

---

### Caso 7 — Ajuste de cantidad (ejemplo: Inventario físico)

**¿Qué es?** Se hace un conteo físico de la bodega y las cantidades reales no coinciden con lo que dice el sistema.

**Ejemplo:**

Un operador cuenta los productos y encuentra:
- **Azúcar**: el sistema dice 50 kg pero en bodega hay solo **45 kg** → hay **5 kg de menos**.
- **Harina**: el sistema dice 30 kg pero en bodega hay **33 kg** → hay **3 kg de más**.

**¿Qué hace el sistema?**

1. El operador registra el conteo físico con las cantidades reales.
2. El sistema compara con lo que tenía registrado.
3. Se generan dos ajustes automáticamente:
   - Un ajuste de **reducción** para el azúcar (−5 kg).
   - Un ajuste de **aumento** para la harina (+3 kg).
4. Cada ajuste queda registrado con fecha, responsable y motivo.
5. A partir de ese momento el inventario refleja la realidad de la bodega.

> Los ajustes de cantidad sí modifican el stock disponible.

---

### Caso 8 — Ajuste de costo (ejemplo: Factura con monto incorrecto)

**¿Qué es?** Se descubre que una compra se registró con el precio equivocado. Las unidades en bodega están correctas, pero el costo del lote está mal.

**Ejemplo:**

Se compró azúcar y se registró a **$0.80/kg** cuando el precio real era **$1.00/kg**. Hay 45 kg en bodega.

**¿Qué hace el sistema?**

1. El operador localiza el lote afectado y registra el costo correcto.
2. El sistema actualiza el costo del lote sin tocar las cantidades disponibles.
3. Esto impacta el costo de venta de las unidades que salgan de ese lote en adelante.
4. El ajuste queda registrado con el costo anterior, el costo nuevo, la diferencia y el responsable.

> Los ajustes de costo **no** modifican el stock disponible, solo el valor del inventario.

---

### Caso 9 — Ajuste de cantidad y costo (ejemplo: Factura con cantidad y precio incorrectos)

**¿Qué es?** Se descubre que una compra se registró con el precio equivocado **y** con una cantidad diferente a la que realmente entró a bodega. Hay que corregir ambas cosas.

**Ejemplo:**

Se compró aceite y se registró: 100 litros a $1.20/lt. En realidad llegaron solo **90 litros** a **$1.50/lt**.

**¿Qué hace el sistema?**

1. El operador detecta la diferencia y registra la cantidad y el precio reales.
2. El sistema genera un ajuste de cantidad (−10 litros) y un ajuste de costo ($1.20 → $1.50/lt).
3. Ambos ajustes quedan registrados con fecha, responsable y motivo vinculados al mismo lote.

> Ambos ajustes pueden hacerse en una sola operación o por separado.

---

## Resumen rápido

| Caso | ¿Qué hago? | ¿Cuándo sale el stock? | Devolución parcial | Regalía (admin) |
|------|-----------|----------------------|-------------------|------------------|
| C1 Reventa | Compro y vendo tal cual | Al confirmar la venta | Sí, devuelve al lote original | Sí, ajuste sin factura |
| C2 Manufactura | Compro MP, produzco y vendo el producto terminado | Al completar la producción | Sí, devuelve al lote de PT | Sí, ajuste sin factura |
| C3 Ensamble | Vendo un producto que se arma al momento con ingredientes | Al confirmar la factura | Sí, revierte ingredientes por receta | Sí, descuenta ingredientes sin factura |
| C4 Variantes | Vendo un producto en diferentes versiones (talla/color) | Al confirmar la venta de la variante | Sí, devuelve a la variante exacta | Sí, ajuste sin factura |
| C5 Pedido configurado | El cliente elige opciones → pasa a producción → se entrega | Al completar la producción | Sí, devuelve unidades de PT | Sí, ajuste sin factura |
| C6 Combo configurado | El cliente arma un combo con varios productos y opciones | Al completar cada producción del combo | Sí, por ítem del combo | Sí, por ítem o combo completo |

### Ajustes

| Caso | ¿Qué corrige? | ¿Modifica stock? | ¿Modifica costo? |
|------|--------------|-----------------|------------------|
| C7 Ajuste de cantidad | Diferencia entre conteo físico y sistema | Sí | No |
| C8 Ajuste de costo | Precio incorrecto en una compra registrada | No | Sí |
| C9 Ajuste de cantidad y costo | Cantidad y precio incorrectos en la misma compra | Sí | Sí |
