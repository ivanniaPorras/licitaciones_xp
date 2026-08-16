# Módulo: Persistencia

> Estado: **terminado** (entrega 4).
> Referencia de navegación: [../README.md](../README.md).

## 1. Propósito

Guardar y recuperar la información del sistema en PostgreSQL, garantizando que las reglas
de negocio que dependen del almacenamiento —unicidad, integridad referencial, montos
positivos, exclusividad del tipo de cambio activo— se cumplan aunque alguien las burle en
las capas superiores.

## 2. Responsabilidades

- Mapear las entidades del dominio a tablas, con los tipos exactos que exige el enunciado.
- Aplicar las migraciones versionadas y los datos semilla.
- Asignar automáticamente las fechas de auditoría.
- Convertir las eliminaciones en borrados lógicos y excluir lo dado de baja de las
  consultas ordinarias.
- Detectar ediciones simultáneas mediante concurrencia optimista.
- Agrupar en transacciones las operaciones que afectan a varios registros.
- Traducir los fallos de PostgreSQL a mensajes comprensibles.

## 3. Dependencias

- `Licitaciones.Domain`: las cinco entidades, `MontoCRC`, `IClock`, `IAuditable` e
  `ISoftDeletable`.
- `Licitaciones.Application`: las interfaces de repositorio y `IUnitOfWork`, que este
  módulo implementa.
- Entity Framework Core 9 y el proveedor Npgsql.

La dirección de la dependencia importa: **la capa de aplicación define el contrato y la
infraestructura lo cumple**, nunca al revés. `Licitaciones.Application` no referencia
Entity Framework Core, y hay una prueba de arquitectura que lo verifica.

## 4. Entradas

| Entrada | Origen | Formato |
|---|---|---|
| Entidades del dominio | Servicios de aplicación | `Agregar(entidad)` en el repositorio |
| Orden de confirmar | Servicios de aplicación | `IUnitOfWork.GuardarCambiosAsync` |
| Operación atómica | Servicios de aplicación | `IUnitOfWork.EjecutarEnTransaccionAsync` |
| Cadena de conexión | Variable de entorno | `ConnectionStrings__Default` |

## 5. Salidas

| Salida | Destino | Formato |
|---|---|---|
| Entidades recuperadas | Servicios de aplicación | Objetos del dominio, nunca filas ni DTO |
| Errores de negocio | Servicios de aplicación | `ReglaNegocioException` con mensaje controlado |
| Conflicto de concurrencia | Servicios de aplicación | `DbUpdateConcurrencyException` |

## 6. Reglas

| ID | Regla | Cómo se garantiza |
|----|-------|-------------------|
| R09 | Nombre de proveedor único | Índice único parcial sobre `nombre_normalizado` |
| R11 | Código de licitación único | Índice único parcial sobre `codigo_normalizado` |
| R05 | Una oferta por proveedor y licitación | Índice único compuesto |
| R19 | Un solo tipo de cambio activo | Índice único parcial con filtro `activo = true` |
| R01, R02 | Montos mayores que cero | Restricciones de verificación |
| R22 | No borrar con dependencias | Claves foráneas restrictivas más borrado lógico |
| — | Auditoría automática | Interceptor sobre el seguimiento de cambios |
| — | Ediciones simultáneas detectadas | Columna de sistema `xmin` como testigo |

El detalle de tipos, índices y decisiones está en [../modelo-datos.md](../modelo-datos.md).

## 7. Errores

Los fallos de PostgreSQL se traducen usando el **nombre de la restricción** que falló, que
es información que este proyecto controla porque define esos nombres explícitamente.

| Restricción | Mensaje |
|---|---|
| `ix_licitaciones_codigo_normalizado` | Ya existe una licitación con ese código. |
| `ix_proveedores_nombre_normalizado` | Ya existe un proveedor con ese nombre. |
| `ix_ofertas_licitacion_proveedor` | Este proveedor ya registró una oferta para esta licitación. |
| `ix_tipos_cambio_unico_activo` | Ya hay un tipo de cambio activo. |
| `ck_ofertas_monto_positivo` | El monto ofertado debe ser mayor que cero. |

Cuando el fallo no corresponde a una restricción conocida, se traduce por su código de
estado SQL: `23505` unicidad, `23503` integridad referencial, `23514` verificación. Si
tampoco encaja, se deja pasar como error no controlado para que el registro del servidor
lo recoja. **El mensaje crudo del motor nunca llega a la persona usuaria.**

## 8. Pruebas

Todas se ejecutan contra **PostgreSQL 16 real en contenedor** mediante Testcontainers.
SQLite y las bases en memoria están prohibidas: buena parte de lo que se verifica —índices
parciales, restricciones de verificación, `numeric` exacto, `xmin`— no existe fuera de
PostgreSQL.

| Prueba | Qué verifica |
|--------|--------------|
| `PersistenciaTests` | Migraciones aplicadas, ida y vuelta de cada entidad, precisión decimal, fechas en UTC, semilla |
| `AuditoriaYBorradoLogicoTests` | Fechas automáticas, baja lógica, conservación de ofertas asociadas |
| `RestriccionesTests` | Los cuatro índices únicos, clave foránea, restricción de verificación por SQL directo |
| `ConcurrenciaYTransaccionesTests` | Conflicto de edición simultánea y reversión de transacción |
| `RepositoriosTests` | Búsqueda del aprobador en los siete límites, unicidad normalizada, monto máximo, reversión |

**Requisito:** Docker debe estar en ejecución. Las pruebas unitarias no lo necesitan.

```bash
dotnet test tests/Licitaciones.IntegrationTests
```
