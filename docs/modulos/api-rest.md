# Módulo: API REST

> Estado: **terminado** (entrega 11).
> Referencia de navegación: [../README.md](../README.md).

## 1. Propósito

Exponer todas las operaciones del sistema como una interfaz de programación estable y
documentada, para que otro sistema pueda automatizarlas sin usar el navegador.

## 2. Responsabilidades

- Publicar los casos de uso de los cinco módulos bajo `api/v1`.
- Traducir el resultado de cada caso de uso al código de estado que le corresponde.
- Devolver siempre objetos de transferencia, nunca entidades de persistencia.
- Uniformar los listados con su paginación, filtrado y ordenamiento.
- Devolver errores comprensibles y seguros, con código propio e identificador de
  correlación.
- Publicar documentación interactiva.

## 3. Dependencias

- `Licitaciones.Application`: los cinco servicios y sus objetos de transferencia.
- `Licitaciones.Infrastructure`: solo para registrar el contexto y los repositorios en el
  arranque.
- `Asp.Versioning` y `Swashbuckle`.

Los controladores no contienen lógica de negocio: cada acción llama a un servicio y traduce
su resultado.

## 4. Entradas

| Entrada | Origen | Formato |
|---|---|---|
| Cuerpos de creación y modificación | Cliente HTTP | JSON con la forma de los `Request` de la capa de aplicación |
| Filtros de listado | Cadena de consulta | `pagina`, `tamano`, `orden`, `busqueda` y los propios de cada recurso |
| Identificadores | Ruta | `Guid`, exigido por la restricción de ruta |

## 5. Salidas

| Salida | Destino | Formato |
|---|---|---|
| Recursos | Cliente HTTP | Los `Response` de la capa de aplicación |
| Listados | Cliente HTTP | `PagedResponse<T>` con `elementos`, `pagina`, `tamano`, `total` y `totalPaginas` |
| Errores | Cliente HTTP | `ProblemDetails` con `code` y `correlationId` |
| Documentación | Navegador | OpenAPI en `/swagger` |

La tabla completa de endpoints vive en [../api.md](../api.md).

## 6. Reglas

| ID | Regla | Dónde vive |
|----|-------|-----------|
| — | Todas las rutas bajo `api/v1` | `[Route("api/v{version:apiVersion}/...")]` en cada controlador |
| — | Ninguna entidad de persistencia se expone | Los servicios solo devuelven registros de `Licitaciones.Application` |
| — | Cada tipo de error tiene su código HTTP | `ControladorApiBase.AProblema` |
| — | Los listados se acotan y responden con su total | `ConsultaPaginada` y `PagedResponse<T>` |
| — | Toda respuesta de error lleva código y correlación | `ControladorApiBase`, `MiddlewareExcepciones` y `RespuestaSolicitudInvalida` |

### La traducción a códigos HTTP ocurre en un solo lugar

`ControladorApiBase.AProblema` convierte el `TipoError` del resultado en 404, 409 o 422.
Ningún controlador decide el código por su cuenta, así que dos módulos no pueden acabar
respondiendo distinto ante la misma clase de fallo.

La distinción entre 409 y 422 es deliberada. **409** es "sus datos están bien, pero el
sistema no está como para hacer esto": un nombre ya tomado, una licitación cerrada, una
transición no permitida. **422** es "el sistema está bien, pero sus datos incumplen una
regla": un monto negativo, una oferta por encima del presupuesto.

### Ningún error sale sin cuerpo

Tres piezas cubren entre las tres todos los caminos posibles:

- `ControladorApiBase` atiende los fallos de negocio que un caso de uso devuelve.
- `RespuestaSolicitudInvalida` sustituye la respuesta 400 que ASP.NET Core produce por
  omisión, que no lleva código propio ni correlación y arrastra los mensajes del enlazador
  de modelos, en inglés y con nombres de tipos internos. En su lugar nombra solo los campos
  que fallaron.
- `MiddlewareExcepciones` atiende lo que se escapa: una regla de negocio que llegó como
  excepción, un fallo no previsto, y también las respuestas que el enrutador deja con el
  código puesto pero sin cuerpo, que son el 404 de una ruta inexistente y el 405 de un
  verbo no admitido.

### El tamaño de página se acota, no se obedece

`ConsultaPaginada` corrige una página menor que 1 a la primera y recorta el tamaño a 100.
Una petición no puede pedir la tabla entera, ni por descuido ni a propósito.

### La documentación se genera del código

La descripción de cada operación sale de los comentarios XML de los controladores, que el
compilador extrae a `Licitaciones.Api.xml` y Swagger incrusta. No hay dos textos que
mantener sincronizados: si el comentario cambia, la documentación cambia con él.

### Un fallo no previsto no revela nada

El detalle completo queda en el registro del servidor con el mismo `correlationId` que
recibe el cliente. La respuesta lleva un mensaje genérico. Nunca salen trazas de pila,
rutas de archivos, consultas ni credenciales.

## 7. Errores

Todas las respuestas de error usan `ProblemDetails` con `title`, `status`, `detail` seguro,
`code` e identificador de correlación.

| Código HTTP | Cuándo | Código del dominio |
|---|---|---|
| 400 | El cuerpo o un parámetro no se pueden interpretar | `SOLICITUD_INVALIDA` |
| 404 | El recurso no existe | El del módulo, por ejemplo `PROVEEDOR_NO_ENCONTRADO` |
| 404 | La ruta no existe | `RUTA_NO_ENCONTRADA` |
| 405 | La ruta no admite ese verbo | `METODO_NO_PERMITIDO` |
| 409 | El estado actual impide la operación | El del módulo, por ejemplo `OFERTA_DUPLICADA` |
| 422 | Los datos incumplen una regla | El del módulo, por ejemplo `TASA_INVALIDA` |
| 500 | Fallo no previsto | `ERROR_INTERNO` |

La lista completa de códigos del dominio está en [../api.md](../api.md#3-códigos-de-estado-y-ejemplos-de-error).

## 8. Pruebas

| Prueba | Tipo | Regla |
|--------|------|-------|
| `ContratoApiTests` | Integración | Versionado, forma y acotación de los listados, códigos de estado y cuerpos de error seguros |
| `ProveedoresEndpointsTests` | Integración | Recorrido completo de proveedores |
| `LicitacionesEndpointsTests` | Integración | Recorrido completo y transiciones de estado |
| `OfertasEndpointsTests` | Integración | Las cuatro reglas de rechazo de ofertas |
| `NivelesAprobacionEndpointsTests` | Integración | Traslapes y resolución del aprobador |
| `TiposCambioEndpointsTests` | Integración | Tasa única activa y conversión |

Todas se ejecutan contra la API real levantada con `WebApplicationFactory` y contra
PostgreSQL 16 real en contenedor. `NingunErrorRevelaDetallesInternos` comprueba
explícitamente que ninguna respuesta de error contenga trazas, consultas ni credenciales.
