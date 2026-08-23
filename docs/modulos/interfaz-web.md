# Módulo: Interfaz web

> Estado: **terminado** (entrega 10).
> Referencia de navegación: [../README.md](../README.md).

## 1. Propósito

Presentar el sistema a quien lo usa desde el navegador: explicar qué hace, dar acceso a
cada sección y permitir el recorrido completo de cada módulo sin conocer la interfaz de
programación.

## 2. Responsabilidades

- Explicar el propósito del sistema y sus reglas en la página inicial.
- Ofrecer navegación a las cinco secciones y a la documentación de la API.
- Alternar entre tema claro y oscuro, y entre colones y dólares.
- Paginar, filtrar y ordenar los cinco listados.
- Mostrar los mensajes de validación junto al campo que los provoca.
- Funcionar sin acceso a Internet.

## 3. Dependencias

- `Licitaciones.Application`: los cinco servicios de aplicación. Los controladores solo
  orquestan.
- `Licitaciones.Web.Vistas`: `ParametrosListado` y `ColumnaOrdenable`, que sostienen los
  listados.
- Bootstrap, jQuery y jquery-validation, versionados en `wwwroot/lib`.

La interfaz **no** conoce la capa de infraestructura ni el dominio salvo por los
enumerados que muestra, como `EstadoLicitacion`.

## 4. Entradas

| Entrada | Origen | Formato |
|---|---|---|
| Formularios de cada módulo | Persona usuaria | Los mismos `Request` que consume la API |
| Filtros de listado | Cadena de consulta | `pagina`, `tamano`, `orden`, `busqueda` y los propios de cada módulo |
| Preferencia de tema | Almacenamiento local del navegador | `light` o `dark` |
| Preferencia de moneda | Almacenamiento local del navegador | `CRC` o `USD` |

## 5. Salidas

| Salida | Destino | Formato |
|---|---|---|
| Páginas HTML | Navegador | Vistas Razor sobre el diseño compartido |
| Mensajes de resultado | `TempData` | Aviso de éxito o de error tras cada operación |
| Errores de validación | `ModelState` | Junto al campo correspondiente |

## 6. Reglas

| ID | Regla | Dónde vive |
|----|-------|-----------|
| — | La página inicial explica el flujo, las ofertas, la mejor oferta, la aprobación y la moneda | `Views/Home/Index.cshtml` |
| — | El menú da acceso a las seis secciones y a la documentación de la API | `_Layout.cshtml` |
| — | La preferencia de tema se conserva y respeta la del sistema operativo | `wwwroot/js/tema.js` |
| — | Los cinco listados paginan, buscan y ordenan | `_ColumnaOrdenable`, `_Paginacion`, `ParametrosListado` |
| — | La columna que ordena se distingue del resto | `ParametrosListado.EstadoDe` y `.orden-activo` |
| — | Sin resultados se muestra un aviso, no una tabla vacía | Cada `Index.cshtml` |

### El tema se decide antes de pintar

`tema.js` se carga **en el encabezado y de forma síncrona**, antes de la hoja de estilos y
del cuerpo. Si se ejecutara al final de la página, el navegador alcanzaría a pintar el tema
por omisión y la corrección se vería como un parpadeo. El guion pone `data-bs-theme` en el
elemento raíz y Bootstrap se encarga del resto.

La primera visita respeta `prefers-color-scheme`, es decir la preferencia del sistema
operativo. En cuanto la persona elige un tema, la elección manda y se guarda; mientras no
elija, la página sigue al sistema si este cambia.

Ningún color se fija a mano en las hojas de estilo propias: se toman de las variables de
Bootstrap. Con un valor literal, el modo oscuro heredaría los tonos del claro y el
contraste del texto dejaría de ser suficiente.

### Los enlaces de un listado conservan los filtros

`ParametrosListado` construye cada enlace de ordenamiento y de paginación **a partir de la
cadena de consulta vigente**, cambiando solo el parámetro que corresponde. Sin eso, pasar a
la página siguiente perdería la búsqueda y los filtros, y la persona vería la lista
completa justo después de haberla filtrado.

Cambiar el ordenamiento vuelve a la primera página, porque quedarse en la página siete de
un listado recién reordenado no muestra nada de lo que se estaba viendo.

### Una sola forma de encabezado ordenable

`_ColumnaOrdenable` dibuja el `<th>` completo: el enlace, la flecha de dirección, la clase
que resalta la columna activa y el atributo `aria-sort`. Los cinco listados lo usan, de modo
que ordenar se comporta y se ve igual en todos y la accesibilidad no depende de que alguien
se acuerde de añadir el atributo.

### El alternador de moneda

Vive en la barra de navegación y lo publica `AlternadorMonedaViewComponent`. Se documenta
en [tipo-cambio.md](tipo-cambio.md).

### Sin dependencias de red

Bootstrap, jQuery y jquery-validation se sirven desde `wwwroot/lib`, y el diagrama del flujo
es un SVG propio en `wwwroot/img`. No se carga ningún recurso desde una red de distribución
de contenido: la aplicación se ve correctamente sin acceso a Internet.

El diagrama usa `currentColor` en trazos y textos, así que se lee igual en tema claro y en
oscuro sin necesidad de tener dos imágenes.

## 7. Errores

| Situación | Qué ve la persona usuaria |
|---|---|
| Regla de negocio incumplida | El mensaje del servicio junto al campo que lo provoca |
| Recurso inexistente | Página 404 del servidor |
| Conflicto de edición simultánea | Aviso comprensible, sin detalles de la base de datos |
| Fallo no previsto | `Views/Shared/Error.cshtml` con solo el identificador de correlación |

Ninguna pantalla muestra trazas de pila ni detalles internos.

## 8. Pruebas

| Prueba | Tipo | Regla |
|--------|------|-------|
| `ContratoApiTests` | Integración | Paginación, filtros y forma de los listados que alimentan las pantallas |

El recorrido en un navegador real —página inicial, cambio de tema, alternancia de moneda y
el ciclo completo de los cinco módulos— se automatiza en la entrega 13 con
`Licitaciones.FunctionalTests`, tal como pide HU-035.
