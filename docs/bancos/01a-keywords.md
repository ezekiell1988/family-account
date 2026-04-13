# 01a — Anexo: Catálogo de Keywords por Template

> Generado el 2026-04-12. Refleja el seed actual de `BankStatementTemplateConfiguration.cs`.  
> Ajustar aquí y luego trasladar los cambios al archivo `.cs` + correr reset de BD.

---

## Referencias rápidas

### Tipos de movimiento (`idBankMovementType`)

| ID | Código | Nombre | Signo |
|---|---|---|---|
| 1 | SAL | Depósito de Salario | Abono |
| 2 | DEP | Depósito en Efectivo | Abono |
| 3 | TRANSF-REC | Transferencia Recibida | Abono |
| 4 | GASTO | Gasto General | Cargo |
| 5 | RET | Retiro en Efectivo | Cargo |
| 6 | PAGO-TC | Pago Tarjeta de Crédito | Cargo |
| 7 | PAGO-PREST | Pago de Préstamo | Cargo |
| 8 | TRANSF-ENV | Transferencia Enviada | Cargo |

### Cuentas contables frecuentes (`idAccountCounterpart`)

| ID | Código | Nombre |
|---|---|---|
| 15 | 4.3 | Otros Ingresos |
| 28 | 2.1.01 | BAC Credomatic – Tarjetas (agrupador) |
| 34 | 1.1.03.01 | BNCR – Cta. CR86… (₡) |
| 42 | 2.2.01.01 | Coopealianza – Préstamo |
| 44 | 4.1.01.01 | ITQS – Salario Ordinario |
| 61 | 5.3.01 | Alimentación |
| 68 | 5.7.01 | Teléfono Celular |
| 69 | 5.8.01 | Netflix |
| 72 | 5.8.04 | Apple iCloud |
| 73 | 5.8.05 | ChatGPT |
| 74 | 5.8.06 | Copilot / Suscripciones Tech |
| 75 | 5.12.01 | Gastos en Pareja |
| 139 | 5.5.07 | Seguro Protección BAC |
| 140 | 5.8.07 | Suscripciones Varias |
| 142 | 5.16.01 | Compras Tecnología (ICON / Apple) |
| 79 | 5.7.02 | Internet |
| 80 | 5.9.01 | AyA |
| 81 | 5.9.02 | CNFL |
| 82 | 5.9.03 | Teléfono Casa |
| 83 | 5.4.02 | Transporte Actividades |
| 96 | 5.12 | Otros (agrupador catch-all) |
| 106 | 1.1.06.01 | Caja CRC (₡) |

### Centros de costo (`idCostCenter`)

| ID | Código | Nombre |
|---|---|---|
| 1 | FAM-KYE | Familia Baltodano Soto (K & E) |
| 2 | FAM-PAPA | Familia Baltodano Cubillo (Papás) |
| 3 | OTROS | Otros |

---

## Template 1 — `BCR-HTML-XLS-V1` · Banco de Costa Rica

| Keywords | Tipo | Cuenta contrapartida | Centro de costo |
|---|---|---|---|
| `SALARIO`, `ITQS`, `IT QUEST`, `NOMINA`, `PLANILLA` | 1 – SAL | 44 – 4.1.01.01 ITQS Salario | — |
| `DEP EFECTIVO`, `DEPOSITO EFECTIVO`, `DEPOSITO EN CAJA` | 2 – DEP | 106 – 1.1.06.01 Caja CRC | — |
| `INTERNET DTR SINPE`, `DTR SINPE`, `SINPE CR`, `TRANSF CREDIT`, `CREDITO SINPE`, `SINPE MOVIL CR`, `ABONO SINPE`, `RECIBO SINPE` | 3 – TRANSF-REC | _(default tipo)_ | — |
| `MOVISTAR`, `KOLBI`, `DB AH TELEF`, `PG AH TIEMPO AIRE TD` | 4 – GASTO | 68 – 5.7.01 Teléfono Celular | — |
| `83681485` | 4 – GASTO | 68 – 5.7.01 Teléfono Celular | 1 – FAM-KYE |
| `22703332` | 4 – GASTO | 82 – 5.9.03 Teléfono Casa | 2 – FAM-PAPA |
| `COMPRAS EN COMERCIOS`, `COMPRA EN COMERCIO`, `COMPRAS COMERC`, `COMPRA COMERC` | 4 – GASTO | 61 – 5.3.01 Alimentación | — |
| `RETIRO ATM`, `RETIRO CAJERO`, `RETIRO EFECTIVO`, `CAJERO AUTOMATICO` | 5 – RET | 106 – 1.1.06.01 Caja CRC | — |
| `PAGO TC`, `PAGO TARJETA`, `TRJ CRED`, `PAGO TARJETA CREDITO`, `PAGO TRJ`, `PAGO TARJETAS`, `TRANSFERENC BANCOBCR` | 6 – PAGO-TC | _(default tipo)_ | — |
| `PAGO PREST`, `CUOTA PREST`, `PAGO PRESTAMO`, `CUOTA PRESTAMO` | 7 – PAGO-PREST | 42 – 2.2.01.01 Coopealianza Préstamo | — |
| `SINPE MOVIL OTRA ENT`, `OTRA ENT`, `TRANSF DEB`, `SINPE DEB`, `DEB SINPE`, `SINPE MOVIL DEB`, `DEBITO SINPE`, `TRANSFERENCIA SINPE DEB`, `CARGO SINPE`, `MONEDERO SINPE MOVIL` | 8 – TRANSF-ENV | _(default tipo)_ | — |

---

## Template 2 — `BAC-TXT-V1` · BAC Credomatic Tarjeta (ambas monedas)

> ⚠️ Template genérico sin moneda específica. Preferir templates 4 (CRC) y 5 (USD).

| Keywords | Tipo | Cuenta contrapartida | Centro de costo |
|---|---|---|---|
| `SU PAGO RECIBIDO GRACIAS` | 3 – TRANSF-REC | _(default tipo)_ | — |
| `UBER`, `DLC*UBER`, `DLC*LYFT`, `BOLT` | 4 – GASTO | 83 – 5.4.02 Transporte Actividades | — |
| `NETFLIX.COM` | 4 – GASTO | 69 – 5.8.01 Netflix | — |
| `APPLE.COM` | 4 – GASTO | 72 – 5.8.04 Apple iCloud | — |
| `GITHUB`, `SPOTIFY`, `YOUTUBE`, `AMAZON` | 4 – GASTO | 140 – 5.8.07 Suscripciones Varias | — |
| `WALMART`, `MAXIPALI`, `MXM `, `SUPER SALON`, `AUTOMERCADO`, `PALI ` | 4 – GASTO | 61 – 5.3.01 Alimentación | — |
| ~~`IVA -`~~ | ~~4 – GASTO~~ | _(eliminada — sub-línea de compra, sin clasificar)_ | — |

---

## Template 3 — `BNCR-CSV-V1` · Banco Nacional de Costa Rica

| Keywords | Tipo | Cuenta contrapartida | Centro de costo |
|---|---|---|---|
| `SALARIO`, `ITQS`, `IT QUEST`, `NOMINA`, `PLANILLA` | 1 – SAL | 44 – 4.1.01.01 ITQS Salario | — |
| `INTERESES GANADOS` | 2 – DEP | _(default tipo)_ | — |
| `TRANSFERENCIA SINPE`, `SINPE MOVIL`, `PAGO TARJETA BAC`, `PAGOTARJETABAC`, `SEMANA MAXIPAL`, `PAGO SERVICIO PROFESIONAL`, `PAGOSERVICIO` | 3 – TRANSF-REC | _(default tipo)_ | — |
| `RETIRO ATM`, `RETIRO CAJERO`, `RETIRO EFECTIVO` | 5 – RET | 106 – 1.1.06.01 Caja CRC | — |
| `PAGO TARJET`, `PAGO TC`, `TARJETA CRED` | 6 – PAGO-TC | _(default tipo)_ | — |
| `PAGO PREST`, `CUOTA PREST`, `PAGO PRESTAMO`, `CUOTA PRESTAMO` | 7 – PAGO-PREST | 42 – 2.2.01.01 Coopealianza Préstamo | — |
| `SINPE MOVIL DEB`, `DEB SINPE`, `CARGO SINPE`, `TRANSF DEB` | 8 – TRANSF-ENV | _(default tipo)_ | — |

---

## Template 4 — `BAC-TXT-CRC-V1` · BAC Credomatic Tarjeta CRC (₡)

| Keywords | Tipo | Cuenta contrapartida | Centro de costo |
|---|---|---|---|
| `SU PAGO RECIBIDO GRACIAS` | 3 – TRANSF-REC | _(default tipo)_ | — |
| `UBER`, `DLC*UBER`, `DLC*LYFT`, `BOLT` | 4 – GASTO | 83 – 5.4.02 Transporte Actividades | — |
| `NETFLIX.COM` | 4 – GASTO | 69 – 5.8.01 Netflix | — |
| `APPLE.COM` | 4 – GASTO | 72 – 5.8.04 Apple iCloud | — |
| `OPENAI`, `CHATGPT` | 4 – GASTO | 73 – 5.8.05 ChatGPT | — |
| `GITHUB`, `MICROSOFT`, `2CO.COM`, `DIGITALOCEAN`, `NEOTHEK`, `GOOGLE` | 4 – GASTO | 74 – 5.8.06 Copilot/Suscripciones Tech | — |
| `SPOTIFY`, `YOUTUBE`, `AMAZON` | 4 – GASTO | 140 – 5.8.07 Suscripciones Varias | — |
| `WALMART`, `MAXIPALI`, `MXM `, `SUPER SALON`, `AUTOMERCADO`, `PALI `, `SIMAN`, `ALMACENES` | 4 – GASTO | 61 – 5.3.01 Alimentación | — |
| `FARMACIA`, `DROGUERIA`, `CLINICA `, `HOSPITAL`, `OPTICA `, `LABORATORIO` | 4 – GASTO | 75 – 5.12.01 Gastos en Pareja | — |
| `FERRETERIA`, `DEPOSITO FERR`, `CONSTRUPLAZA` | 4 – GASTO | 75 – 5.12.01 Gastos en Pareja | — |
| `SEGURO PROTECCION`, `SEGURO DE VIDA`, `PRIMA SEGURO`, `INS ` | 4 – GASTO | 139 – 5.5.07 Seguro Protección BAC | — |
| ~~`IVA -`~~ | ~~4 – GASTO~~ | _(eliminada — sub-línea de compra, sin clasificar)_ | — |
| `TRASLADO SALDO REVOLUTIVO`, `CUOTA:` | 7 – PAGO-PREST | 42 – 2.2.01.01 Coopealianza Préstamo | — |

---

## Template 5 — `BAC-TXT-USD-V1` · BAC Credomatic Tarjeta USD ($)

| Keywords | Tipo | Cuenta contrapartida | Centro de costo |
|---|---|---|---|
| `SU PAGO RECIBIDO GRACIAS` | 3 – TRANSF-REC | _(default tipo)_ | — |
| `UBER`, `DLC*UBER`, `DLC*LYFT`, `BOLT` | 4 – GASTO | 83 – 5.4.02 Transporte Actividades | — |
| `NETFLIX.COM` | 4 – GASTO | 69 – 5.8.01 Netflix | — |
| `APPLE.COM` | 4 – GASTO | 72 – 5.8.04 Apple iCloud | — |
| `OPENAI`, `CHATGPT`, `GAMMA.APP` | 4 – GASTO | 73 – 5.8.05 ChatGPT | — |
| `GITHUB`, `JETBRAINS`, `MICROSOFT`, `DIGITALOCEAN`, `2CO.COM`, `NEOTHEK`, `GOOGLE` | 4 – GASTO | 74 – 5.8.06 Copilot/Suscripciones Tech | — |
| `SPOTIFY`, `YOUTUBE`, `AMAZON` | 4 – GASTO | 140 – 5.8.07 Suscripciones Varias | — |
| `ICON CC RETAIL` | 4 – GASTO | 142 – 5.16.01 Compras Tecnología (ICON / Apple) | — |
| `WALMART`, `SIMAN` | 4 – GASTO | 61 – 5.3.01 Alimentación | — |
| `SEGURO PROTECCION`, `SEGURO DE VIDA`, `PRIMA SEGURO` | 4 – GASTO | 139 – 5.5.07 Seguro Protección BAC | — |
| ~~`IVA -`~~ | ~~4 – GASTO~~ | _(eliminada — sub-línea de compra, sin clasificar)_ | — |
| `TRASLADO SALDO REVOLUTIVO`, `CUOTA:` | 7 – PAGO-PREST | 42 – 2.2.01.01 Coopealianza Préstamo | — |

---

## Template 6 — `BAC-XLS-V1` · BAC Credomatic Cuenta de Ahorro/Débito (XLS)

| Keywords | Tipo | Cuenta contrapartida | Centro de costo |
|---|---|---|---|
| `SALARIO`, `ITQS`, `IT QUEST`, `NOMINA`, `PLANILLA` | 1 – SAL | 44 – 4.1.01.01 ITQS Salario | — |
| `DEP_ATM`, `TATMFULL`, `DEPOSITO ATM` | 2 – DEP | 106 – 1.1.06.01 Caja CRC | — |
| `TEF DE:`, `DTR SINPE`, `SINPE REC`, `ABONO SINPE`, `CREDITO SINPE` | 3 – TRANSF-REC | _(default tipo)_ | — |
| `COOPEALIANZA`, `CAJA AHORRO` | 7 – PAGO-PREST | 42 – 2.2.01.01 Coopealianza Préstamo | — |
| `PAGO `, `SINPE MOVIL PAGO_TARJETA` | 6 – PAGO-TC | _(default tipo)_ | — |
| `DTR:`, `RETIRO CAJERO`, `RETIRO ATM`, `RETIRO EFECTIVO` | 8 – TRANSF-ENV | _(default tipo)_ | — |

---

## Notas y pendientes

- Las filas con `_(default tipo)_` en cuenta usan la `idAccountCounterpart` definida en el tipo de movimiento (ver tabla de tipos).
- Las filas con `—` en centro de costo no aplican CC; el usuario puede asignarlo manualmente durante clasificación.
- `matchMode` es `Any` en todos los casos (al menos una keyword debe coincidir).
- La comparación es insensible a mayúsculas y acentos (normalización NFD).
- **Revisar:** Templates 2, 3, 4, 5, 6 no tienen reglas con `idCostCenter` aún — agregar si aplica.
