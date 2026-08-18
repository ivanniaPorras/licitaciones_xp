# Módulo: Ofertas

> Estado: **terminado** (entrega 7).
> Referencia de navegación: [../README.md](../README.md).

## 1. Propósito

Registrar las propuestas económicas que los proveedores presentan a una licitación, y
determinar cuál de ellas conviene más.

## 2. Responsabilidades

- Validar que la oferta cumpla todas las reglas antes de persistirse.
- Garantizar una única oferta por par licitación y proveedor.
- Admitir ofertas solo sobre licitaciones publicadas y vigentes.
- Preservar las ofertas de licitaciones cerradas como evidencia inmutable.
- Seleccionar la mejor oferta y clasificar el ahorro.

## 3. Dependencias

- `Licitaciones.Domain.Ofertas`: `Oferta`, `EvaluadorMejorOferta`, `ClasificadorAhorro`.
- `Licitaciones.Domain.Tiempo`: `IClock`, para decidir el vencimiento.
- Módulo Licitaciones: estado, presupuesto y fecha de cierre.
- Módulo Proveedores: existencia del proveedor.

## 4. Entradas

| Entrada | Origen | Formato |
|---|---|---|
| `CrearOfertaRequest` | MVC / API | `{ licitacionId, proveedorId, montoOfertadoCRC }` |
| `CrearOfertaEnLicitacionRequest` | API anidada | `{ proveedorId, montoOfertadoCRC }` — la licitación viene de la ruta |
| `ActualizarOfertaRequest` | MVC / API | `{ montoOfertadoCRC }` |
| `ConsultaOfertas` | MVC / API | `{ pagina, tamano, orden, busqueda, licitacionId, proveedorId }` |

## 5. Salidas

| Salida | Destino | Formato |
|---|---|---|
| `OfertaResponse` | MVC / API | Incluye el código de la licitación y el nombre del proveedor |
| `PagedResponse<OfertaResponse>` | MVC / API | Listado paginado |

## 6. Reglas

| ID | Regla | Dónde vive |
|----|-------|-----------|
| R02 | El monto debe ser mayor que cero | `MontoCRC` + restricción de verificación |
| R03 | La oferta no puede superar el presupuesto | `OfertaService.ComprobarMonto` |
| R04 | Una oferta igual al presupuesto es válida | La comparación es `>`, no `>=` |
| R05 | Una oferta por proveedor y licitación | `OfertaService` + índice único compuesto |
| R06 | Solo sobre licitación publicada | `OfertaService.ComprobarQueAdmiteOfertas` |
| R07 | Solo antes de la fecha de cierre | `Licitacion.EstaCerradaFuncionalmente` |
| R12 | La mejor oferta es la de menor monto | `EvaluadorMejorOferta` |
| R13 | En empate gana la registrada primero | `EvaluadorMejorOferta` |
| R23 | Inmutable si la licitación está cerrada | `OfertaService.CargarParaModificarAsync` |

### Matriz de operaciones

| Estado de la licitación | Crear | Editar | Eliminar |
|---|---|---|---|
| Borrador | ❌ `LICITACION_NO_PUBLICADA` | ❌ | ❌ |
| Publicada y vigente | ✅ | ✅ | ✅ |
| Publicada pero vencida | ❌ `LICITACION_CERRADA` | ❌ `OFERTA_INMUTABLE` | ❌ `OFERTA_INMUTABLE` |
| Cerrada | ❌ `LICITACION_CERRADA` | ❌ `OFERTA_INMUTABLE` | ❌ `OFERTA_INMUTABLE` |

**Las tres operaciones quedan bloqueadas, no solo la creación.** Editar y eliminar
comparten exactamente la misma condición, así que la comprobación vive en un único método
—`CargarParaModificarAsync`— en lugar de estar duplicada. Si la regla cambia, cambia en un
solo sitio.

### Siempre se consulta el cierre funcional

Ninguna validación mira `Estado == Cerrada` por su cuenta: todas llaman a
`EstaCerradaFuncionalmente`, que además comprueba la fecha. Así una licitación que sigue
marcada como Publicada porque nadie actualizó el campo tampoco admite ofertas una vez
vencido el plazo.

### La fecha de registro no la pone el cliente

`OfertaService` toma el instante de `IClock`, no del cuerpo de la petición. Es la fecha que
decide el orden de llegada y, con él, el desempate de la mejor oferta: dejarla en manos de
quien envía la petición permitiría colarse en el desempate.

### Por qué la oferta no tiene borrado lógico

A diferencia de licitaciones y proveedores, la oferta o existe o se elimina físicamente,
y solo mientras su licitación siga vigente. Una vez cerrada, es inmutable y se conserva
entera. No hace falta una marca de baja porque no hay ningún momento en que se quiera
ocultar sin borrar.

## 7. Errores

| Código | HTTP | Mensaje |
|--------|------|---------|
| `OFERTA_DUPLICADA` | 409 | Este proveedor ya registró una oferta para esta licitación. |
| `OFERTA_SUPERA_PRESUPUESTO` | 422 | La oferta no puede superar el presupuesto de la licitación. |
| `MONTO_INVALIDO` | 422 | El monto debe ser mayor que cero. |
| `LICITACION_NO_PUBLICADA` | 409 | La licitación no está publicada. |
| `LICITACION_CERRADA` | 409 | La licitación ya cerró; no se admiten más ofertas. |
| `OFERTA_INMUTABLE` | 409 | Las ofertas de licitaciones cerradas no pueden modificarse. |
| `OFERTA_NO_ENCONTRADA` | 404 | La oferta solicitada no existe. |

## 8. Pruebas

| Prueba | Tipo | Regla |
|--------|------|-------|
| `EvaluadorMejorOfertaTests` | Unitaria | R12, R13 |
| `ClasificadorAhorroTests` | Unitaria | R14, con las fronteras de 10 %, 9,99 % y 0 % |
| `MontoCRCTests` | Unitaria | R02 |
| `OfertaServiceTests` | Unitaria | R02–R07, R23 y la matriz completa |
| `RestriccionesTests` | Integración | R05 en la base |
| `OfertasEndpointsTests` | Integración | La matriz completa por HTTP |

Los casos frontera de vencimiento están probados en los tres puntos exactos: un segundo
antes del cierre se acepta, en el instante del cierre se rechaza, y un segundo después
se rechaza.
