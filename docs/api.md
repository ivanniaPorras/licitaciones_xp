# API REST

Volver al [índice de la documentación](README.md).

Interfaz de programación del sistema. Permite automatizar todas las operaciones sin usar
el navegador.

## 1. Principios del contrato

- **Versionado.** Todas las rutas viven bajo `api/v1`. La versión va en la ruta y no en un
  encabezado, de modo que una petición se puede copiar, pegar y reproducir tal cual.
- **Objetos de transferencia.** Ninguna entidad de persistencia se expone. Lo que entra y
  sale son registros declarados en `Licitaciones.Application`, así que cambiar una columna
  no cambia el contrato.
- **Colones como fuente de verdad.** Todos los montos se expresan en colones
  costarricenses. El equivalente en dólares se pide al recurso de conversión y nunca se
  almacena.
- **Errores uniformes.** Todo fallo devuelve `ProblemDetails` con un código propio del
  dominio y un identificador de correlación. Se detalla en la sección 3.
- **Documentación interactiva.** La API se explora y se prueba desde `/swagger`, enlazado
  en el menú de la aplicación web.

### Parámetros comunes de los listados

| Parámetro | Valor por omisión | Notas |
|---|---|---|
| `pagina` | 1 | Empieza en 1. Un valor menor se corrige a 1. |
| `tamano` | 20 | Se acota a 100 como máximo, para que ninguna petición pida la tabla entera. |
| `orden` | según el recurso | Con la forma `campo:asc` o `campo:desc`. |
| `busqueda` | sin filtro | Término de búsqueda. Su significado depende del recurso. |

Todos los listados responden con la misma envoltura:

```json
{
  "elementos": [],
  "pagina": 1,
  "tamano": 20,
  "total": 37,
  "totalPaginas": 2
}
```

## 2. Tabla de endpoints

### Proveedores

| Verbo | Ruta | Respuesta | Notas |
|---|---|---|---|
| `GET` | `/api/v1/proveedores` | 200 | Busca por nombre. Orden: `nombre`, `creacion`. |
| `GET` | `/api/v1/proveedores/{id}` | 200 · 404 | |
| `POST` | `/api/v1/proveedores` | 201 · 409 · 422 | Devuelve la ubicación del recurso creado. |
| `PUT` | `/api/v1/proveedores/{id}` | 200 · 404 · 409 · 422 | |
| `DELETE` | `/api/v1/proveedores/{id}` | 204 · 404 | Borrado lógico. |
| `GET` | `/api/v1/proveedores/{id}/ofertas` | 200 · 404 | Historial de participación. |

### Licitaciones

| Verbo | Ruta | Respuesta | Notas |
|---|---|---|---|
| `GET` | `/api/v1/licitaciones` | 200 | Filtra por `estado`. Orden: `codigo`, `fechaCierre`. |
| `GET` | `/api/v1/licitaciones/{id}` | 200 · 404 | |
| `POST` | `/api/v1/licitaciones` | 201 · 409 · 422 | El identificador lo genera el sistema. |
| `PUT` | `/api/v1/licitaciones/{id}` | 200 · 404 · 409 · 422 | No admite bajar el presupuesto por debajo de una oferta. |
| `PATCH` | `/api/v1/licitaciones/{id}/estado` | 200 · 404 · 409 · 422 | Solo transiciones permitidas. |
| `DELETE` | `/api/v1/licitaciones/{id}` | 204 · 404 | Borrado lógico si tiene ofertas. |
| `GET` | `/api/v1/licitaciones/{id}/ofertas` | 200 · 404 | |
| `POST` | `/api/v1/licitaciones/{id}/ofertas` | 201 · 404 · 409 · 422 | |
| `GET` | `/api/v1/licitaciones/{id}/mejor-oferta` | 200 · 404 | Incluye ahorro, clasificación y aprobador. |

### Ofertas

| Verbo | Ruta | Respuesta | Notas |
|---|---|---|---|
| `GET` | `/api/v1/ofertas` | 200 | Filtra por `licitacionId` y `proveedorId`. Busca por código o proveedor. Orden: `monto`, `fecha`. |
| `GET` | `/api/v1/ofertas/{id}` | 200 · 404 | |
| `POST` | `/api/v1/ofertas` | 201 · 404 · 409 · 422 | |
| `PUT` | `/api/v1/ofertas/{id}` | 200 · 404 · 409 · 422 | Rechazado si la licitación ya cerró. |
| `DELETE` | `/api/v1/ofertas/{id}` | 204 · 404 · 409 | Rechazado si la licitación ya cerró. |

### Niveles de aprobación

| Verbo | Ruta | Respuesta | Notas |
|---|---|---|---|
| `GET` | `/api/v1/niveles-aprobacion` | 200 | Busca por aprobador. Orden: `montoMinimo`, `aprobador`. |
| `GET` | `/api/v1/niveles-aprobacion/{id}` | 200 · 404 | |
| `GET` | `/api/v1/niveles-aprobacion/aprobador?monto=` | 200 · 422 | Resuelve el nivel aplicable a un monto. |
| `POST` | `/api/v1/niveles-aprobacion` | 201 · 409 · 422 | Rechaza traslapes y un segundo rango abierto. |
| `PUT` | `/api/v1/niveles-aprobacion/{id}` | 200 · 404 · 409 · 422 | |
| `DELETE` | `/api/v1/niveles-aprobacion/{id}` | 204 · 404 | |

### Tipo de cambio

| Verbo | Ruta | Respuesta | Notas |
|---|---|---|---|
| `GET` | `/api/v1/tipos-cambio` | 200 | Busca por año de vigencia. Orden: `vigencia`, `tasa`. |
| `GET` | `/api/v1/tipos-cambio/{id}` | 200 · 404 | |
| `GET` | `/api/v1/tipos-cambio/vigente` | 200 · 409 | 409 si no hay ninguna tasa activa. |
| `GET` | `/api/v1/tipos-cambio/conversion?monto=` | 200 · 409 · 422 | Devuelve la tasa usada y su fecha. |
| `POST` | `/api/v1/tipos-cambio` | 201 · 422 | La tasa nace fuera de uso. |
| `PUT` | `/api/v1/tipos-cambio/{id}` | 200 · 404 · 422 | |
| `POST` | `/api/v1/tipos-cambio/{id}/activar` | 200 · 404 | Retira de uso la tasa anterior. |
| `DELETE` | `/api/v1/tipos-cambio/{id}` | 204 · 404 | |

## 3. Códigos de estado y ejemplos de error

| Código | Cuándo se usa |
|---|---|
| 200 | Consulta o modificación completada. |
| 201 | Recurso creado. La cabecera `Location` apunta a su ubicación. |
| 204 | Eliminación completada. Sin cuerpo. |
| 400 | La solicitud no se pudo interpretar: falta un campo o su tipo no encaja. |
| 404 | El recurso o la ruta no existen. |
| 405 | La ruta existe pero no admite ese verbo. |
| 409 | El estado actual del sistema impide la operación. |
| 422 | Los datos son interpretables pero incumplen una regla de negocio. |

La distinción entre 409 y 422 es deliberada: **409** significa "sus datos están bien, pero
el sistema no está como para hacer esto" —un nombre ya tomado, una licitación cerrada, una
transición no permitida—; **422** significa "el sistema está bien, pero sus datos incumplen
una regla" —un monto negativo, una oferta por encima del presupuesto—.

### Forma de toda respuesta de error

```json
{
  "title": "Regla de negocio incumplida",
  "status": 422,
  "detail": "La oferta no puede superar el presupuesto de la licitación.",
  "instance": "/api/v1/ofertas",
  "code": "OFERTA_SUPERA_PRESUPUESTO",
  "correlationId": "0HN7...:00000003"
}
```

- `detail` está redactado para la persona usuaria y nunca revela funcionamiento interno.
- `code` es estable: los clientes pueden ramificar sobre él aunque cambie la redacción del
  mensaje.
- `correlationId` es el mismo valor con el que el fallo quedó registrado en el servidor, de
  modo que un reporte se puede rastrear sin exponer nada.

### Códigos de error del dominio

| Código | HTTP |
|---|---|
| `PROVEEDOR_DUPLICADO` | 409 |
| `NOMBRE_PROVEEDOR_INVALIDO` | 422 |
| `PROVEEDOR_NO_ENCONTRADO` | 404 |
| `CODIGO_LICITACION_DUPLICADO` | 409 |
| `LICITACION_NO_ENCONTRADA` | 404 |
| `MONTO_INVALIDO` | 422 |
| `TRANSICION_INVALIDA` | 409 |
| `FECHA_CIERRE_EN_EL_PASADO` | 422 |
| `PRESUPUESTO_MENOR_QUE_OFERTA` | 422 |
| `OFERTA_DUPLICADA` | 409 |
| `OFERTA_SUPERA_PRESUPUESTO` | 422 |
| `LICITACION_NO_PUBLICADA` | 422 |
| `LICITACION_CERRADA` | 422 |
| `OFERTA_NO_ENCONTRADA` | 404 |
| `OFERTA_INMUTABLE` | 409 |
| `RANGO_TRASLAPADO` | 409 |
| `RANGO_ABIERTO_DUPLICADO` | 409 |
| `RANGO_INVALIDO` | 422 |
| `NIVEL_APROBACION_NO_ENCONTRADO` | 404 |
| `SIN_NIVEL_APLICABLE` | 422 |
| `TASA_INVALIDA` | 422 |
| `SIN_TIPO_CAMBIO_ACTIVO` | 409 |
| `TIPO_CAMBIO_NO_ENCONTRADO` | 404 |
| `SOLICITUD_INVALIDA` | 400 |
| `RUTA_NO_ENCONTRADA` | 404 |
| `METODO_NO_PERMITIDO` | 405 |
| `REGLA_NEGOCIO` | 409 |
| `ERROR_INTERNO` | 500 |

Un fallo no previsto devuelve `ERROR_INTERNO` con un mensaje genérico. La excepción
completa queda únicamente en el registro del servidor, junto al mismo `correlationId`.

## 4. Colección reproducible de solicitudes

[`assets/licitaciones.http`](assets/licitaciones.http) recorre el flujo completo en
diecisiete bloques encadenados: registrar proveedores, crear y publicar una licitación,
registrar una oferta válida, provocar los cuatro rechazos previstos, consultar la mejor
oferta con su clasificación y su aprobador, administrar el tipo de cambio, leer un monto en
dólares, recorrer los listados con sus filtros y comprobar la forma de cada error.

Se ejecuta con la extensión **REST Client** de Visual Studio Code o con la ventana de
peticiones de Visual Studio. Los bloques se encadenan por variables, así que la primera vez
hay que lanzarlos en orden. El anfitrión se cambia en la variable `@host` de la cabecera del
archivo.
