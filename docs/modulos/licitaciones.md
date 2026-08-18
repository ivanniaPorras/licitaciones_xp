# Módulo: Licitaciones

> Estado: **terminado** (entrega 6).
> Referencia de navegación: [../README.md](../README.md).

## 1. Propósito

Administrar los procesos de compra: su identificación, su presupuesto, el período durante
el cual reciben ofertas y su avance por el ciclo Borrador → Publicada → Cerrada.

## 2. Responsabilidades

- Garantizar que el código de cada licitación sea único aunque se escriba distinto.
- Decidir qué transiciones de estado son válidas, en un único punto del sistema.
- Determinar si la licitación sigue admitiendo ofertas, considerando estado y fecha.
- Impedir que el presupuesto baje por debajo de una oferta ya recibida.
- Presentar la mejor oferta con su ahorro y su clasificación.

## 3. Dependencias

- `Licitaciones.Domain.Licitaciones`: `Licitacion`, `MaquinaEstadosLicitacion`,
  `NormalizadorCodigo`.
- `Licitaciones.Domain.Ofertas`: `EvaluadorMejorOferta`, `ClasificadorAhorro`.
- `Licitaciones.Domain.Tiempo`: `IClock`, para decidir el vencimiento.
- `Licitaciones.Application.Persistencia`: `ILicitacionRepository`, `IOfertaRepository`,
  `IUnitOfWork`.

## 4. Entradas

| Entrada | Origen | Formato |
|---|---|---|
| `CrearLicitacionRequest` | MVC / API | `{ codigo, titulo, presupuestoEstimadoCRC, fechaCierre }` |
| `ActualizarLicitacionRequest` | MVC / API | Los mismos campos |
| `CambiarEstadoRequest` | MVC / API | `{ estado, motivo }` |
| `ConsultaLicitaciones` | MVC / API | `{ pagina, tamano, orden, busqueda, estado }` |

## 5. Salidas

| Salida | Destino | Formato |
|---|---|---|
| `LicitacionResponse` | MVC / API | Incluye `cerradaFuncionalmente`, que es el dato que decide si se admiten operaciones |
| `MejorOfertaResponse` | MVC / API | `{ oferta, presupuesto, porcentajeAhorro, clasificacion }` |
| `PagedResponse<LicitacionResponse>` | MVC / API | Listado paginado |

## 6. Reglas

| ID | Regla | Dónde vive |
|----|-------|-----------|
| R11 | Código único ignorando espacios y mayúsculas | `NormalizadorCodigo` + índice único parcial |
| R01 | Presupuesto mayor que cero | `MontoCRC` + restricción de verificación |
| R20 | Solo las transiciones permitidas | `MaquinaEstadosLicitacion` |
| R08 | Cierre funcional al alcanzarse la fecha | `Licitacion.EstaCerradaFuncionalmente` |
| R21 | El presupuesto no baja por debajo de una oferta existente | `LicitacionService.ActualizarAsync` |
| — | No se publica con fecha de cierre vencida | `LicitacionService.CambiarEstadoAsync` |

### Decisión: no se implementa la reapertura

El enunciado admite reabrir una licitación cerrada únicamente bajo una regla aprobada
previamente por la persona docente. **No se implementa.** `Cerrada` es un estado terminal:
`MaquinaEstadosLicitacion` no declara ninguna transición desde él, la vista de detalle no
ofrece botones y la API responde 409.

### Por qué no se publica una licitación vencida

Publicar un proceso cuyo plazo ya pasó produciría una licitación que nace cerrada
funcionalmente: aparecería como Publicada pero rechazaría toda oferta. Se rechaza antes,
con el código `FECHA_CIERRE_EN_EL_PASADO`.

### Por qué el presupuesto no puede bajar de una oferta recibida

Una oferta que era válida cuando se presentó no puede invalidarse retroactivamente. Al
editar, el servicio consulta la oferta más alta y rechaza cualquier presupuesto menor,
indicando la cifra concreta en el mensaje. Un presupuesto **igual** a la oferta más alta sí
se acepta: la regla es "no puede superar", no "debe ser menor".

### La interfaz no decide las transiciones

La vista de detalle pinta un botón por cada estado que devuelve
`MaquinaEstadosLicitacion.TransicionesDesde`. Si mañana cambia la tabla de transiciones, la
interfaz se adapta sola y no hay una segunda copia de la regla que mantener sincronizada.

## 7. Errores

| Código | HTTP | Mensaje |
|--------|------|---------|
| `CODIGO_LICITACION_DUPLICADO` | 409 | Ya existe una licitación con ese código. |
| `TRANSICION_INVALIDA` | 409 | Transición de estado no permitida. |
| `MONTO_INVALIDO` | 422 | El monto debe ser mayor que cero. |
| `PRESUPUESTO_MENOR_QUE_OFERTA` | 422 | El presupuesto no puede ser menor que la oferta más alta registrada (… CRC). |
| `FECHA_CIERRE_EN_EL_PASADO` | 422 | No se puede publicar una licitación cuya fecha de cierre ya pasó. |
| `LICITACION_NO_ENCONTRADA` | 404 | La licitación solicitada no existe. |

## 8. Pruebas

| Prueba | Tipo | Regla |
|--------|------|-------|
| `MaquinaEstadosLicitacionTests` | Unitaria | R20 |
| `CierreFuncionalLicitacionTests` | Unitaria | R08 |
| `NormalizadorCodigoTests` | Unitaria | R11 |
| `LicitacionServiceTests` | Unitaria | R11, R01, R20, R21 y publicación vencida |
| `RestriccionesTests` | Integración | R11 en la base |
| `LicitacionesEndpointsTests` | Integración | Códigos HTTP y transiciones prohibidas |
