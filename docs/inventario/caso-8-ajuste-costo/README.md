# Caso 8 — Ajuste de costo (ejemplo: Factura con monto incorrecto)

**¿Qué es?** Se descubre que una compra se registró con el precio equivocado. Las unidades en bodega están correctas, pero el costo del lote está mal.

## Pasos

1. El operador identifica el lote afectado por el error de precio.
2. Registra el costo correcto en el sistema.
3. El sistema actualiza el costo del lote sin modificar las cantidades disponibles.
4. El ajuste impacta el costo de venta de las unidades que salgan de ese lote en adelante.
5. Queda registrado con el costo anterior, el costo nuevo, la diferencia y el responsable.

## Ejemplo concreto

- Se compró azúcar y se registró a **$0.80/kg**, pero el precio real era **$1.00/kg**.
- Hay 45 kg en bodega.
- El operador corrige el costo → el lote queda valorizado a $1.00/kg.
- Las cantidades en bodega (45 kg) no cambian.

---

## Consideraciones

- Los ajustes de costo **no modifican el stock disponible**.
- Solo corrigen el valor (costo) del inventario.
- Quedan visibles en el historial del lote para auditoría.
