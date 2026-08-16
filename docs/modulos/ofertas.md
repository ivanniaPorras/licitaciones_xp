# Módulo: Ofertas

> Estado: **evaluación y clasificación terminadas** (entrega 3). Las validaciones de
> registro, la persistencia, la interfaz y la API se completan en las entregas 4 y 7.
> Referencia de navegación: [../README.md](../README.md).

## 1. Propósito

Registrar las propuestas económicas que los proveedores presentan a una licitación, y
determinar cuál de ellas conviene más.

## 2. Responsabilidades

- Validar que la oferta cumpla todas las reglas antes de persistirse *(entrega 7)*.
- Garantizar una única oferta por par licitación y proveedor *(entrega 7)*.
- Seleccionar la mejor oferta de forma determinista.
- Calcular el ahorro y traducirlo a una etiqueta comprensible.
- Preservar las ofertas de licitaciones cerradas como evidencia inmutable *(entrega 7)*.

## 3. Dependencias

- `Licitaciones.Domain.Dinero`: `MontoCRC` para el monto ofertado.
- Módulo Licitaciones: estado, presupuesto y vencimiento.
- Módulo Proveedores: existencia del proveedor.

## 4. Entradas

| Entrada | Origen | Formato |
|---|---|---|
| Licitación, proveedor, monto y fecha de registro | MVC / API *(entrega 7)* | `Oferta.Crear(licitacionId, proveedorId, montoCRC, fechaRegistro)` |
| Conjunto de ofertas de una licitación | Repositorio | `EvaluadorMejorOferta.Seleccionar(ofertas)` |
| Presupuesto y mejor oferta | Servicio de aplicación | `ClasificadorAhorro.Clasificar(presupuesto, mejorOferta)` |

## 5. Salidas

| Salida | Destino | Formato |
|---|---|---|
| Mejor oferta | Vista de la licitación y API | `Oferta?` — nulo si no hay ofertas |
| Clasificación | Vista de la licitación y API | `ResultadoClasificacion(Etiqueta, PorcentajeAhorro)` |

## 6. Reglas

| ID | Regla | Dónde vive |
|----|-------|-----------|
| R02 | El monto ofertado debe ser mayor que cero | `MontoCRC` |
| R12 | La mejor oferta es la de menor monto | `EvaluadorMejorOferta` |
| R13 | En empate gana la registrada primero | `EvaluadorMejorOferta` |
| R14 | Las cuatro etiquetas de clasificación | `ClasificadorAhorro` |
| R03 | La oferta no puede superar el presupuesto *(entrega 7)* | pendiente |
| R05 | Una oferta por proveedor y licitación *(entrega 7)* | pendiente |
| R06 | Solo sobre licitación publicada *(entrega 7)* | pendiente |
| R07 | Solo antes de la fecha de cierre *(entrega 7)* | pendiente |
| R23 | Inmutable si la licitación está cerrada *(entrega 7)* | pendiente |

### Orden de desempate

`EvaluadorMejorOferta` ordena por monto, luego por fecha de registro en tiempo universal y
por último por identificador. El tercer criterio **no tiene significado de negocio**: está
únicamente para que el resultado sea el mismo sin importar en qué orden devuelva las filas
la base de datos. Sin él, dos ofertas idénticas en monto y en instante de registro podrían
producir ganadoras distintas en dos consultas consecutivas.

### Por qué el ahorro cero se decide comparando montos

La etiqueta "Oferta válida sin ahorro" corresponde al caso en que la oferta **iguala** al
presupuesto. Decidirla a partir del porcentaje redondeado sería incorrecto: con un
presupuesto de 1 000 000,00 CRC, una oferta de 999 999,99 CRC produce un ahorro de
0,000001 %, que redondeado a dos decimales es 0,00 % y haría parecer que no hubo ahorro.
La comparación se hace por lo tanto sobre los montos y no sobre el porcentaje. El
porcentaje redondeado se usa solo para mostrarlo y para contrastarlo con el umbral del
10 %, que es inclusivo.

## 7. Errores

| Código | HTTP | Mensaje |
|--------|------|---------|
| `OFERTA_DUPLICADA` | 409 | Este proveedor ya registró una oferta para esta licitación. |
| `OFERTA_SUPERA_PRESUPUESTO` | 422 | La oferta no puede superar el presupuesto de la licitación. |
| `LICITACION_NO_PUBLICADA` | 409 | La licitación no está publicada. |
| `LICITACION_CERRADA` | 409 | La licitación ya cerró; no se admiten más ofertas. |
| `OFERTA_INMUTABLE` | 409 | Las ofertas de licitaciones cerradas no pueden modificarse. |

## 8. Pruebas

| Prueba | Tipo | Regla |
|--------|------|-------|
| `EvaluadorMejorOfertaTests` | Unitaria | R12, R13 |
| `ClasificadorAhorroTests` | Unitaria | R14 |
| `MontoCRCTests` | Unitaria | R02 |

`ClasificadorAhorroTests` cubre explícitamente las tres fronteras exigidas: ahorro de
exactamente 10 %, ahorro de 9,99 % y oferta igual al presupuesto.
