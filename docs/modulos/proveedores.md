# Módulo: Proveedores

> Estado: **terminado** (entrega 5).
> Referencia de navegación: [../README.md](../README.md).

## 1. Propósito

Mantener el catálogo de empresas y personas que pueden presentar ofertas, garantizando que
cada una figure una sola vez.

## 2. Responsabilidades

- Reconocer que dos nombres escritos de forma distinta designan al mismo proveedor.
- Rechazar nombres con caracteres que no corresponden a una razón social.
- Conservar el nombre tal como lo escribió la persona usuaria, sin imponerle un formato.
- Dar de baja de forma lógica, conservando las ofertas asociadas.
- Consultar las ofertas presentadas por un proveedor.

## 3. Dependencias

- `Licitaciones.Domain.Proveedores`: `Proveedor`, `NormalizadorNombreProveedor`,
  `ValidadorNombreProveedor`.
- `Licitaciones.Application.Persistencia`: `IProveedorRepository`, `IOfertaRepository`,
  `IUnitOfWork`.

No conoce el módulo de licitaciones. Es el módulo más independiente del sistema.

## 4. Entradas

| Entrada | Origen | Formato |
|---|---|---|
| `CrearProveedorRequest` | MVC / API | `{ nombre }` |
| `ActualizarProveedorRequest` | MVC / API | `{ nombre }` |
| `ConsultaProveedores` | MVC / API | `{ pagina, tamano, orden, busqueda }` |

## 5. Salidas

| Salida | Destino | Formato |
|---|---|---|
| `ProveedorResponse` | MVC / API | `{ id, nombre, cantidadOfertas, createdAt, updatedAt }` |
| `PagedResponse<ProveedorResponse>` | MVC / API | `{ elementos, pagina, tamano, total, totalPaginas }` |
| `OfertaResponse` | MVC / API | Ofertas del proveedor con el código de su licitación |

## 6. Reglas

| ID | Regla | Dónde vive |
|----|-------|-----------|
| R09 | El nombre es único tras normalizar | `ProveedorService` + índice único parcial |
| R10 | Solo se admiten letras, números, espacios, punto, coma y paréntesis | `ValidadorNombreProveedor` |
| R22 | No se elimina físicamente un proveedor con ofertas | Borrado lógico + clave foránea restrictiva |

### Validación en tres capas

| Capa | Mecanismo |
|---|---|
| Interfaz | Atributos `required`, `maxlength` y `pattern` en el formulario |
| Servidor | `ProveedorService` consulta `ExisteNombreAsync` antes de persistir |
| PostgreSQL | Índice único parcial sobre `nombre_normalizado` |

La tercera capa no es redundante: entre la consulta del servidor y la escritura hay una
ventana en la que otra petición puede insertar el mismo nombre. Si eso ocurre, el índice
dispara y `TraductorErroresPostgres` devuelve el mismo mensaje controlado.

### Por qué la baja no comprueba si hay ofertas

El borrado es **lógico**: la fila se conserva con su fecha de baja y desaparece de los
listados por el filtro global. Las ofertas que la referencian siguen siendo legibles, así
que no hace falta bloquear la operación. La clave foránea restrictiva impide, además, que
alguien la borre físicamente por otra vía.

### Al editar, el proveedor no compite consigo mismo

`ExisteNombreAsync` recibe el identificador que se está editando y lo excluye de la
comparación. Sin eso, guardar un proveedor sin cambiarle el nombre se rechazaría como
duplicado de sí mismo.

## 7. Errores

| Código | HTTP | Mensaje |
|--------|------|---------|
| `PROVEEDOR_DUPLICADO` | 409 | Ya existe un proveedor con ese nombre. |
| `NOMBRE_PROVEEDOR_INVALIDO` | 422 | El nombre solo admite letras, números, espacios, punto, coma y paréntesis. |
| `PROVEEDOR_NO_ENCONTRADO` | 404 | El proveedor solicitado no existe. |

## 8. Pruebas

| Prueba | Tipo | Regla |
|--------|------|-------|
| `NormalizadorNombreProveedorTests` | Unitaria | R09 |
| `ValidadorNombreProveedorTests` | Unitaria | R10 |
| `ProveedorServiceTests` | Unitaria | R09, R10, R22 |
| `RestriccionesTests` | Integración | R09 en la base |
| `RepositoriosTests` | Integración | Unicidad normalizada y exclusión al editar |
| `ProveedoresEndpointsTests` | Integración | Códigos HTTP y `ProblemDetails` |
