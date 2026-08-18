# Módulo: Niveles de aprobación

> Estado: **terminado** (entrega 8).
> Referencia de navegación: [../README.md](../README.md).

## 1. Propósito

Determinar quién tiene autoridad para aprobar un monto, a partir de una tabla de rangos
que el cliente puede modificar sin que el programa cambie.

## 2. Responsabilidades

- Mantener los rangos de monto y su instancia aprobadora.
- Garantizar que los rangos no se traslapen entre sí.
- Garantizar que a lo sumo un rango quede abierto por arriba.
- Resolver, dado un monto, qué nivel le corresponde.

## 3. Dependencias

- `Licitaciones.Domain.Aprobacion`: `NivelAprobacion` con `Cubre` y `SeTraslapaCon`.
- `Licitaciones.Domain.Dinero`: `MontoCRC`.
- `Licitaciones.Application.Persistencia`: `INivelAprobacionRepository`, `IUnitOfWork`.

El módulo de licitaciones **le pide** el aprobador de la mejor oferta a través de
`INivelAprobacionService`. No consulta la tabla por su cuenta: la política de autorización
es propiedad de este módulo.

## 4. Entradas

| Entrada | Origen | Formato |
|---|---|---|
| `CrearNivelAprobacionRequest` | MVC / API | `{ montoMinimoCRC, montoMaximoCRC, aprobador }` |
| `ActualizarNivelAprobacionRequest` | MVC / API | Los mismos campos |
| Monto a aprobar | Módulo Licitaciones / API | `ObtenerAprobadorAsync(montoCRC)` |

## 5. Salidas

| Salida | Destino | Formato |
|---|---|---|
| `NivelAprobacionResponse` | MVC / API | `{ id, montoMinimoCRC, montoMaximoCRC, esRangoAbierto, aprobador }` |
| Aprobador de la mejor oferta | `MejorOfertaResponse.Aprobador` | Texto, o `null` si ningún rango cubre el monto |

## 6. Reglas

| ID | Regla | Dónde vive |
|----|-------|-----------|
| R15 | El aprobador se obtiene consultando la tabla | `NivelAprobacionRepository.ObtenerAplicableAsync` |
| R16 | Los rangos no pueden traslaparse | `NivelAprobacion.SeTraslapaCon` + `ComprobarConvivencia` |
| R17 | Solo puede existir un rango sin monto máximo | `ComprobarConvivencia` |
| — | Monto mínimo mayor que cero y máximo no menor que el mínimo | `NivelAprobacion.Crear` + restricción de verificación |

### El aprobador nunca sale de un `if`

La consulta es `WHERE monto >= monto_minimo AND (monto_maximo IS NULL OR monto <= monto_maximo)`
ordenada por monto mínimo. Cambiar la política de autorización —añadir un escalón, mover
un umbral, renombrar una instancia— es editar filas, no tocar ni recompilar el programa.
Es un punto que el enunciado evalúa de forma explícita.

### Ambos límites son inclusivos

Un monto de 999 999,99 CRC corresponde a "Encargado de área" y uno de 1 000 000,00 CRC ya
corresponde a "Gerencia". Los rangos de la semilla encajan exactamente sin dejar huecos.

### Por qué la comprobación va en transacción

Entre comprobar que un rango no se traslapa y guardarlo hay una ventana en la que otra
petición podría insertar un rango que invalide la comprobación. `CrearAsync` y
`ActualizarAsync` envuelven toda la operación en una transacción para cerrarla.

### Al editar, el nivel no compite consigo mismo

`ComprobarConvivencia` excluye el identificador que se está editando. Sin eso, cualquier
rango se traslaparía consigo mismo y sería imposible cambiarle siquiera el nombre del
aprobador.

### Semilla

| Monto mínimo CRC | Monto máximo CRC | Aprobador |
|---|---|---|
| 0,01 | 999 999,99 | Encargado de área |
| 1 000 000,00 | 9 999 999,99 | Gerencia |
| 10 000 000,00 | *sin límite* | Junta Directiva |

## 7. Errores

| Código | HTTP | Mensaje |
|--------|------|---------|
| `RANGO_TRASLAPADO` | 409 | El rango se traslapa con un nivel existente. |
| `RANGO_ABIERTO_DUPLICADO` | 409 | Ya existe un nivel sin monto máximo. |
| `RANGO_INVALIDO` | 422 | El monto máximo no puede ser menor que el monto mínimo. |
| `MONTO_INVALIDO` | 422 | El monto debe ser mayor que cero. |
| `SIN_NIVEL_APLICABLE` | 422 | Ningún nivel de aprobación cubre ese monto. |
| `NIVEL_APROBACION_NO_ENCONTRADO` | 404 | El nivel de aprobación solicitado no existe. |

Cuando ningún rango cubre el monto se devuelve un mensaje controlado, no una excepción sin
manejar; en la vista de la mejor oferta aparece "Ningún nivel de aprobación cubre este
monto".

## 8. Pruebas

| Prueba | Tipo | Regla |
|--------|------|-------|
| `EntidadesDominioTests` | Unitaria | Invariantes del rango y `Cubre` |
| `NivelAprobacionServiceTests` | Unitaria | R15, R16, R17 y la edición sin autotraslape |
| `RepositoriosTests` | Integración | R15 contra la tabla sembrada |
| `NivelesAprobacionEndpointsTests` | Integración | R15 por HTTP y el aprobador en la mejor oferta |

Los siete límites de la semilla —0,01 / 500 000 / 999 999,99 / 1 000 000,00 /
9 999 999,99 / 10 000 000,00 / 50 000 000— se prueban explícitamente en los tres niveles:
servicio, repositorio y endpoint.
