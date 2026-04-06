# Caso 3 — Ensamble en venta (ejemplo: Hot dog)

**¿Qué es?** No produzco con anticipación. Los ingredientes se descuentan en el mismo momento en que confirmo el pedido.

## Pasos

1. Tengo en bodega: **panes, salchichas, mostaza, catsup**.
2. El cliente pide **3 hot dogs**.
3. Creo el pedido con la línea de Hot Dog y lo confirmo enviando `{ "idWarehouse": N }`.
4. El sistema ejecuta automáticamente:
   - Crea una Orden de Producción: consume ingredientes por FEFO y genera el lote Hot Dog.
   - Marca el pedido como Completado.
   - Genera y confirma la factura de venta. El lote PT queda vinculado a la línea.
5. La respuesta del `confirm` devuelve el `idSalesInvoice` generado.

**Sin `idWarehouse` en el confirm:** el pedido queda solo en estado `Confirmado` y se puede procesar manualmente paso a paso.

**Si cancelo la factura:** el lote PT se revierte a su cantidad original y los asientos contables se anulan.

**Devolución parcial:** si el cliente devuelve 1 hot dog, se registra un ajuste de inventario con los ingredientes proporcionales (ajuste de entrada) vinculado al lot correspondiente.

---

## Diferencia con Manufactura

- En manufactura la producción es planificada y separada de la venta: se produce para stock y luego se vende desde ese stock.
- En ensamble se produce y vende en un solo flujo automático: el pedido dispara todo.
