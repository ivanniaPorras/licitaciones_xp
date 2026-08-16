# Módulo: Niveles de aprobación

> Estado: **entidad de dominio terminada** (entrega 3). La búsqueda del aprobador contra la
> tabla, la validación de traslape y el CRUD se completan en la entrega 8.
> Referencia de navegación: [../README.md](../README.md).

## 1. Propósito

Determinar quién tiene autoridad para aprobar un monto, a partir de una tabla de rangos
que el cliente puede modificar sin que el programa cambie.

## 2. Responsabilidades

- Mantener los rangos de monto y su instancia aprobadora.
- Garantizar que los rangos no se traslapen entre sí.
- Garantizar que a lo sumo un rango quede abierto por arriba.
- Resolver, dado un monto, qué nivel le corresponde *(entrega 8)*.

## 3. Dependencias

- `Licitaciones.Domain.Dinero`: `MontoCRC` para los límites del rango.
- Ningún otro módulo. El módulo de licitaciones lo consulta para mostrar el aprobador de
  la mejor oferta.

## 4. Entradas

| Entrada | Origen | Formato |
|---|---|---|
| Monto mínimo, monto máximo opcional y aprobador | MVC / API *(entrega 8)* | `NivelAprobacion.Crear(minimo, maximo, aprobador)` |
| Monto a aprobar | Servicio de aplicación *(entrega 8)* | `NivelAprobacion.Cubre(MontoCRC)` |

## 5. Salidas

| Salida | Destino | Formato |
|---|---|---|
| Aprobador aplicable | Vista de la licitación y API | `string Aprobador` |
| Comprobación de traslape | Servicio de aplicación | `bool SeTraslapaCon(NivelAprobacion)` |

## 6. Reglas

| ID | Regla | Dónde vive |
|----|-------|-----------|
| R15 | El aprobador se obtiene consultando la tabla, nunca con condicionales encadenados | `NivelAprobacionService` *(entrega 8)* |
| R16 | Los rangos no pueden traslaparse | `NivelAprobacion.SeTraslapaCon` |
| R17 | Solo puede existir un rango sin monto máximo | `NivelAprobacionService` *(entrega 8)* |
| — | El monto mínimo debe ser mayor que cero y el máximo no puede ser menor que el mínimo | `NivelAprobacion.Crear` |

### Ambos límites son inclusivos

`Cubre` acepta el monto cuando es mayor o igual que el mínimo y menor o igual que el
máximo. Esto hace que los rangos de la semilla encajen exactamente sin dejar huecos: un
monto de 999 999,99 CRC corresponde a "Encargado de área" y uno de 1 000 000,00 CRC ya
corresponde a "Gerencia".

### Criterio de traslape

Dos rangos se traslapan cuando el mínimo de cada uno no supera al máximo del otro,
tratando la ausencia de máximo como infinito. La comprobación vive en la entidad para que
la regla sea verificable sin base de datos; el servicio de aplicación la aplicará contra
todos los demás rangos dentro de una transacción.

### Semilla

| Monto mínimo CRC | Monto máximo CRC | Aprobador |
|---|---|---|
| 0,01 | 999 999,99 | Encargado de área |
| 1 000 000,00 | 9 999 999,99 | Gerencia |
| 10 000 000,00 | sin límite | Junta Directiva |

## 7. Errores

| Código | HTTP | Mensaje |
|--------|------|---------|
| `RANGO_TRASLAPADO` | 409 | El rango se traslapa con un nivel existente. |
| `RANGO_ABIERTO_DUPLICADO` | 409 | Ya existe un nivel sin monto máximo. |
| `RANGO_INVALIDO` | 422 | El monto máximo no puede ser menor que el monto mínimo. |

## 8. Pruebas

| Prueba | Tipo | Regla |
|--------|------|-------|
| `EntidadesDominioTests` | Unitaria | R16 y las invariantes del rango |

Los límites de la semilla —0,01 / 999 999,99 / 1 000 000,00 / 9 999 999,99 /
10 000 000,00 / 50 000 000,00— se prueban explícitamente en la entrega 8, cuando exista la
consulta contra la tabla.
