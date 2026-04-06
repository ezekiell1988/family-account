# Caso 4 — Variantes (ejemplo: Ropa por talla y color)

**¿Qué es?** Vendo un mismo producto en varias versiones (talla S, M, L / color azul, rojo). Cada versión tiene su propio inventario separado.

## Pasos

1. En el catálogo creo el producto padre: **"Camisa Oxford"**.
2. Defino sus variantes: Talla S Azul, Talla M Azul, Talla L Azul, Talla S Rojo, etc.
3. Compro **20 camisas Talla M Azul** → solo ese inventario sube.
4. El cliente quiere una **Talla L Rojo** → si no hay stock de esa variante exacta, el sistema avisa.
5. Al facturar, el operador elige la variante exacta (Talla M Azul) y el sistema descuenta de ese inventario.

## Ejemplo concreto

- Talla S Azul: 5 unidades disponibles.
- Talla M Azul: 20 unidades disponibles.
- Talla L Rojo: 0 unidades → no se puede vender.

---

## Anulación y devolución

**Si cancelo la factura:** las unidades de la variante vendida vuelven a su inventario específico automáticamente.

**Devolución parcial:** si el cliente devuelve solo algunas unidades de una variante (por ejemplo, 2 de 5 camisas Talla M Azul facturadas), el operador registra la devolución indicando variante y cantidad. El sistema suma esas 2 unidades al inventario de esa variante exacta y genera una nota de crédito.

---

## Documento de regalía (solo administrador)

Un usuario administrador puede emitir un **documento de regalía** sobre una variante específica. Este documento:
- **No genera venta ni factura** al cliente.
- **Descuenta el inventario** de esa variante como un ajuste de cantidad.
- Queda registrado con motivo y responsable para auditoría.

**Ejemplo:** el administrador regala 1 camisa Talla M Azul a un empleado → el sistema descuenta 1 unidad del inventario de esa variante y registra el ajuste como regalía.
