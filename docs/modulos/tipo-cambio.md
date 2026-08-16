# Módulo: Tipo de cambio

> Estado: **entidad de dominio terminada** (entrega 3). El CRUD, la activación en
> transacción y el servicio de conversión se completan en la entrega 9.
> Referencia de navegación: [../README.md](../README.md).

## 1. Propósito

Mantener localmente la tasa de conversión entre colones y dólares, y permitir mostrar los
montos en dólares sin alterar los valores almacenados.

## 2. Responsabilidades

- Guardar tasas con su fecha de vigencia.
- Garantizar que solo una tasa esté activa a la vez *(entrega 9)*.
- Convertir montos de colones a dólares *(entrega 9)*.
- Funcionar sin acceso a Internet.

## 3. Dependencias

- `Licitaciones.Domain.Dinero`: comparte espacio con `MontoCRC`, aunque la tasa no es un
  monto.
- Ningún otro módulo depende de este para operar: la conversión es una representación
  añadida, no un dato del negocio.

## 4. Entradas

| Entrada | Origen | Formato |
|---|---|---|
| Tasa y fecha de vigencia | MVC / API *(entrega 9)* | `TipoCambio.Crear(crcPorUsd, fechaVigencia)` |
| Orden de activación | MVC / API *(entrega 9)* | `TipoCambio.Activar()` / `Desactivar()` |

## 5. Salidas

| Salida | Destino | Formato |
|---|---|---|
| Monto convertido | Vistas y API *(entrega 9)* | Monto en dólares con dos decimales |
| Tasa usada y su fecha | Vistas y API *(entrega 9)* | Se muestra siempre junto al monto convertido |

## 6. Reglas

| ID | Regla | Dónde vive |
|----|-------|-----------|
| — | La tasa debe ser mayor que cero | `TipoCambio.Crear` |
| R19 | Solo un tipo de cambio activo a la vez | Índice único parcial y transacción *(entrega 9)* |
| R18 | Dólares = colones dividido entre la tasa activa | `ConversionMonedaService` *(entrega 9)* |
| — | La fecha del tipo de cambio se muestra junto al monto convertido | Vistas *(entrega 10)* |

### Los colones son la única fuente de verdad

La conversión a dólares **nunca se persiste**. Se calcula al mostrar y se redondea a dos
decimales alejándose de cero. Guardar el equivalente en dólares crearía un segundo valor
que quedaría desactualizado en cuanto cambiara la tasa, y abriría la puerta a que dos
partes del sistema informaran cifras distintas para el mismo monto.

### Por qué la tasa admite cuatro decimales y un monto solo dos

Un monto es una cantidad de dinero que debe cuadrar al céntimo, y por eso se limita a dos
decimales. La tasa no es dinero sino un factor de conversión: restringirla a dos decimales
introduciría un error apreciable al dividir cifras grandes.

### Sin dependencias de red

El sistema no consulta ningún servicio externo de tasas. La tasa la administra una persona
de la organización, y la semilla incluye un registro activo para que la aplicación
funcione desde la primera ejecución sin conexión.

## 7. Errores

| Código | HTTP | Mensaje |
|--------|------|---------|
| `TASA_INVALIDA` | 422 | El tipo de cambio debe ser mayor que cero. |
| `SIN_TIPO_CAMBIO_ACTIVO` | 409 | No hay un tipo de cambio activo para realizar la conversión. |

## 8. Pruebas

| Prueba | Tipo | Regla |
|--------|------|-------|
| `TipoCambioTests` | Unitaria | Tasa positiva, precisión y marca de activo |

La exclusividad del registro activo y la conversión se prueban en la entrega 9, la primera
contra PostgreSQL real porque depende de un índice único parcial y de una transacción.
