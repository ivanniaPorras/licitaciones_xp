# Módulo: Licitaciones

> Estado: **capa de dominio terminada** (entrega 3). La persistencia, la interfaz y la API
> se completan en las entregas 4 y 6. Referencia de navegación: [../README.md](../README.md).

## 1. Propósito

Administrar los procesos de compra: su identificación, su presupuesto, el período durante
el cual reciben ofertas y su avance por el ciclo Borrador → Publicada → Cerrada.

## 2. Responsabilidades

- Garantizar que el código de cada licitación sea único aunque se escriba con distintas
  mayúsculas o espacios.
- Decidir qué transiciones de estado son válidas, en un único punto del sistema.
- Determinar si la licitación sigue admitiendo ofertas, considerando tanto su estado como
  su fecha de cierre.
- Impedir que el presupuesto baje por debajo de una oferta ya recibida *(entrega 6)*.

## 3. Dependencias

- `Licitaciones.Domain.Dinero`: `MontoCRC` para el presupuesto.
- `Licitaciones.Domain.Tiempo`: `IClock` para decidir el vencimiento.
- No depende de ningún otro módulo. El módulo de ofertas sí depende de este.

## 4. Entradas

| Entrada | Origen | Formato |
|---|---|---|
| Código, título, presupuesto y fecha de cierre | MVC / API *(entrega 6)* | `Licitacion.Crear(codigo, titulo, presupuestoCRC, fechaCierre)` |
| Estado destino | MVC / API *(entrega 6)* | `Licitacion.CambiarEstado(EstadoLicitacion)` |

## 5. Salidas

| Salida | Destino | Formato |
|---|---|---|
| Licitación creada | Repositorio | Entidad `Licitacion` con su código normalizado |
| Decisión de vencimiento | Módulo de ofertas | `bool EstaCerradaFuncionalmente(IClock)` |
| Transiciones posibles | Interfaz | `ImmutableArray<EstadoLicitacion> TransicionesDesde(estado)` |

## 6. Reglas

| ID | Regla | Dónde vive |
|----|-------|-----------|
| R11 | El código es único ignorando espacios laterales y mayúsculas | `NormalizadorCodigo` |
| R20 | Solo se permiten las transiciones Borrador→Publicada, Borrador→Cerrada y Publicada→Cerrada | `MaquinaEstadosLicitacion` |
| R08 | Una licitación cuya fecha de cierre se alcanzó está cerrada aunque su estado diga Publicada | `Licitacion.EstaCerradaFuncionalmente` |
| R01 | El presupuesto debe ser mayor que cero | `MontoCRC` |
| R21 | El presupuesto no puede reducirse por debajo de la oferta más alta *(entrega 6)* | pendiente |

### Decisión: no se implementa la reapertura

El enunciado admite reabrir una licitación cerrada únicamente bajo una regla aprobada
previamente por la persona docente. **No se implementa.** `Cerrada` es un estado terminal:
`MaquinaEstadosLicitacion` no declara ninguna transición desde él, y la prueba
`Cerrada_NoAdmiteNingunaTransicion` lo verifica.

### Decisión: el límite de la fecha de cierre es inclusivo hacia el rechazo

Cuando la hora actual es **exactamente igual** a la fecha de cierre, la licitación ya se
considera cerrada. La comparación es `reloj.UtcNow >= FechaCierre`, y se hace siempre en
tiempo universal para que el huso horario de quien opera no altere el resultado.

## 7. Errores

| Código | HTTP | Mensaje |
|--------|------|---------|
| `TRANSICION_INVALIDA` | 409 | Transición de estado no permitida. |
| `CODIGO_DUPLICADO` | 409 | Ya existe una licitación con ese código. |
| `PRESUPUESTO_INVALIDO` | 422 | El monto debe ser mayor que cero. |

Los códigos y su asociación con el estado HTTP se formalizan en la entrega 11.

## 8. Pruebas

| Prueba | Tipo | Regla |
|--------|------|-------|
| `MaquinaEstadosLicitacionTests` | Unitaria | R20 |
| `CierreFuncionalLicitacionTests` | Unitaria | R08 |
| `NormalizadorCodigoTests` | Unitaria | R11 |

Las tres cubren los casos frontera: transiciones prohibidas, transición al mismo estado,
instante exacto del cierre, comparación entre husos horarios y variantes de escritura del
código.
