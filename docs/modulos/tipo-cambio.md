# Módulo: Tipo de cambio

> Estado: **terminado** (entrega 9).
> Referencia de navegación: [../README.md](../README.md).

## 1. Propósito

Mantener localmente la tasa de conversión entre colones y dólares, y permitir mostrar los
montos en dólares sin alterar los valores almacenados.

## 2. Responsabilidades

- Guardar tasas con su fecha de vigencia.
- Garantizar que solo una tasa esté activa a la vez.
- Convertir montos de colones a dólares, devolviendo siempre la tasa usada y su fecha.
- Funcionar sin acceso a Internet.

## 3. Dependencias

- `Licitaciones.Domain.Dinero`: `TipoCambio` y `MontoCRC`.
- `Licitaciones.Application.Persistencia`: `ITipoCambioRepository`, `IUnitOfWork`.

Ningún otro módulo depende de este para operar: la conversión es una representación
añadida, no un dato del negocio. Si no hay tasa activa, el resto del sistema sigue
funcionando en colones.

## 4. Entradas

| Entrada | Origen | Formato |
|---|---|---|
| `CrearTipoCambioRequest` | MVC / API | `{ crCporUSD, fechaVigencia }` |
| `ActualizarTipoCambioRequest` | MVC / API | Los mismos campos |
| Orden de activación | MVC / API | `ActivarAsync(id)` |
| Monto por convertir | MVC / API | `ConvertirAsync(montoCRC)` |

## 5. Salidas

| Salida | Destino | Formato |
|---|---|---|
| `TipoCambioResponse` | MVC / API | `{ id, crCporUSD, fechaVigencia, activo, createdAt, updatedAt }` |
| `ConversionResponse` | MVC / API | `{ montoCRC, montoUSD, crCporUSD, fechaVigencia }` |

## 6. Reglas

| ID | Regla | Dónde vive |
|----|-------|-----------|
| — | La tasa debe ser mayor que cero y admite hasta cuatro decimales | `TipoCambio.Crear` y `CambiarTasa` |
| R19 | Solo un tipo de cambio activo a la vez | `TipoCambioService.ActivarAsync` en transacción, más el índice único parcial `ix_tipos_cambio_unico_activo` |
| R18 | Dólares = colones dividido entre la tasa activa | `ConversionMonedaService.ConvertirAsync` |
| — | La tasa usada y su fecha se muestran junto al monto convertido | `ConversionResponse` y el alternador de la barra de navegación |
| — | La vigencia es una fecha de calendario, anclada a medianoche universal | `TipoCambio.Crear` |

### Los colones son la única fuente de verdad

La conversión a dólares **nunca se persiste**. Se calcula al mostrar y se redondea a dos
decimales alejándose de cero. Guardar el equivalente en dólares crearía un segundo valor
que quedaría desactualizado en cuanto cambiara la tasa, y abriría la puerta a que dos
partes del sistema informaran cifras distintas para el mismo monto.

El redondeo se hace alejándose de cero y no al par más cercano, que es lo que hace
`Math.Round` por omisión: con el redondeo al par, un mismo monto podría mostrarse hacia
arriba o hacia abajo según cuál fuera el dígito anterior.

### Por qué la tasa admite cuatro decimales y un monto solo dos

Un monto es una cantidad de dinero que debe cuadrar al céntimo, y por eso se limita a dos
decimales. La tasa no es dinero sino un factor de conversión: restringirla a dos decimales
introduciría un error apreciable al dividir cifras grandes.

### Una tasa nace fuera de uso

Registrar una tasa no la pone a convertir. Si al guardarla quedara activa de inmediato,
crear un registro cambiaría sin querer todos los montos que ya se están mostrando en
pantalla. Activarla es una acción aparte, con confirmación previa en la interfaz.

### Por qué la activación va en transacción y en dos pasos

Activar una tasa son dos cambios: retirar de uso la anterior y poner la nueva. Si el
proceso se interrumpiera entre ambos, el sistema quedaría con dos tasas activas o con
ninguna, y los dos estados incumplen la regla. `ActivarAsync` envuelve la operación
completa en una transacción.

Dentro de esa transacción, la tasa anterior se retira **y se confirma** antes de marcar la
nueva. El índice único parcial de PostgreSQL se comprueba en cada instrucción y no al
cerrar la transacción, así que las dos marcas no pueden coexistir ni por un instante.
La base garantiza la regla por sí sola aunque una condición de carrera burlara la
comprobación del servidor.

### La vigencia es una fecha, no un instante

Quien administra la tasa escribe un día en el formulario, no una hora. `TipoCambio.Crear`
conserva ese día y lo ancla a medianoche en tiempo universal, y las pantallas lo muestran
sin convertirlo a hora local. Sin esa normalización, una vigencia del 1 de enero guardada
a medianoche universal se leería como 31 de diciembre desde Costa Rica.

Es la única fecha del sistema que se trata así. `FechaCierre` y `FechaRegistro` sí son
instantes y se siguen mostrando en hora local.

### Sin dependencias de red

El sistema no consulta ningún servicio externo de tasas. La tasa la administra una persona
de la organización, y la semilla incluye un registro activo —512,0000 CRC por USD, vigente
desde el 1 de enero de 2026— para que la aplicación funcione desde la primera ejecución
sin conexión.

### El alternador de la interfaz

El alternador vive en la barra de navegación y lo publica `AlternadorMonedaViewComponent`,
que consulta la tasa vigente. Las vistas marcan cada monto con `data-crc`, que conserva el
valor en colones sin formato; al pasar a dólares, el guion de `site.js` repite la misma
división y muestra la tasa usada con su fecha de vigencia. Volver a colones restituye el
texto original, de modo que el valor almacenado nunca se pierde por redondeos sucesivos.
La conversión que manda sigue siendo la del servidor: es la que expone la API y la que
está cubierta por pruebas.

Sin tasa activa el alternador no se dibuja y todas las pantallas siguen mostrando colones.

## 7. Errores

| Código | HTTP | Mensaje |
|--------|------|---------|
| `TASA_INVALIDA` | 422 | El tipo de cambio debe ser mayor que cero. |
| `TASA_INVALIDA` | 422 | El tipo de cambio no puede tener más de cuatro decimales. |
| `SIN_TIPO_CAMBIO_ACTIVO` | 409 | No hay un tipo de cambio activo para realizar la conversión. |
| `TIPO_CAMBIO_NO_ENCONTRADO` | 404 | El tipo de cambio solicitado no existe. |
| `MONTO_INVALIDO` | 422 | El monto debe ser mayor que cero. |

La ausencia de tasa activa se trata como conflicto y no como validación: los datos de la
petición están bien, lo que falta es un estado del sistema que alguien debe corregir
registrando y activando una tasa.

## 8. Pruebas

| Prueba | Tipo | Regla |
|--------|------|-------|
| `TipoCambioTests` | Unitaria | Tasa positiva, precisión, cambio de tasa y marca de activo |
| `TipoCambioServiceTests` | Unitaria | R19, tasa positiva y traducción de los errores del dominio |
| `ConversionMonedaServiceTests` | Unitaria | R18, redondeo alejándose de cero y ausencia de tasa activa |
| `TiposCambioEndpointsTests` | Integración | R18 y R19 por HTTP contra PostgreSQL real |

La exclusividad del registro activo se comprueba contra PostgreSQL real porque depende de
un índice único parcial y de una transacción: fuera del motor real, la regla no puede
verificarse de verdad.
