# Historias de usuario

Volver al [índice de la documentación](README.md).

Las historias están escritas desde la perspectiva de quien necesita el resultado. Cada
una tiene prioridad, estimación en puntos, la iteración en la que se compromete y
criterios de aceptación verificables.

La columna **Trazabilidad** de cada historia se completa conforme avanza el trabajo:
enlaza las pruebas que la verifican, los commits del ciclo rojo → verde → refactorización
y el documento de `/docs` que la describe.

## Escala de estimación

Se usa una escala reducida de puntos de historia: **1, 2, 3, 5, 8**. Un punto equivale
aproximadamente a media jornada de trabajo de una programadora. Cualquier historia
estimada en más de 8 puntos se divide antes de comprometerla.

## Escala de prioridad

Definida por el cliente:

- **Alta** — sin esto el sistema no cumple su propósito ni pasa la revisión.
- **Media** — necesario para la entrega completa, pero puede moverse de iteración.
- **Baja** — mejora la experiencia; se sacrifica primero si la velocidad no alcanza.

---

## Tabla resumen

| ID | Historia | Prioridad | Puntos | Iteración | Estado |
|---|---|---|---|---|---|
| HU-001 | Estructura del proyecto e integración continua | Alta | 3 | 1 | Terminada |
| HU-002 | Ciclo de estados de una licitación | Alta | 5 | 1 | Terminada |
| HU-003 | Cierre funcional al alcanzarse la fecha | Alta | 3 | 1 | Terminada |
| HU-004 | Código de licitación único | Alta | 3 | 1 | Terminada |
| HU-005 | Nombre de proveedor único | Alta | 5 | 1 | Terminada |
| HU-006 | Caracteres permitidos en el nombre del proveedor | Alta | 2 | 1 | Terminada |
| HU-007 | Montos siempre mayores que cero | Alta | 2 | 1 | Terminada |
| HU-008 | Selección de la mejor oferta | Alta | 3 | 1 | Terminada |
| HU-009 | Clasificación del ahorro obtenido | Alta | 3 | 1 | Terminada |
| HU-010 | Persistencia en PostgreSQL con datos iniciales | Alta | 5 | 2 | Terminada |
| HU-011 | Registro automático de fechas y borrado lógico | Media | 3 | 2 | Terminada |
| HU-012 | Protección ante ediciones simultáneas | Media | 3 | 2 | Terminada |
| HU-013 | Administrar proveedores | Alta | 5 | 2 | Pendiente |
| HU-014 | Consultar las ofertas de un proveedor | Media | 2 | 2 | Pendiente |
| HU-015 | Administrar licitaciones | Alta | 5 | 2 | Pendiente |
| HU-016 | Cambiar el estado de una licitación | Alta | 3 | 2 | Pendiente |
| HU-017 | No reducir el presupuesto bajo una oferta existente | Alta | 3 | 2 | Pendiente |
| HU-018 | Registrar una oferta válida | Alta | 5 | 3 | Pendiente |
| HU-019 | Rechazar una oferta superior al presupuesto | Alta | 2 | 3 | Pendiente |
| HU-020 | Impedir una segunda oferta del mismo proveedor | Alta | 3 | 3 | Pendiente |
| HU-021 | Impedir ofertas fuera del período de recepción | Alta | 3 | 3 | Pendiente |
| HU-022 | Preservar las ofertas de licitaciones cerradas | Alta | 3 | 3 | Pendiente |
| HU-023 | Consultar la mejor oferta con su clasificación | Alta | 3 | 3 | Pendiente |
| HU-024 | Administrar niveles de aprobación sin traslape | Alta | 5 | 3 | Pendiente |
| HU-025 | Conocer quién debe aprobar un monto | Alta | 3 | 3 | Pendiente |
| HU-026 | Administrar el tipo de cambio vigente | Alta | 5 | 3 | Pendiente |
| HU-027 | Ver los montos en dólares | Alta | 3 | 3 | Pendiente |
| HU-028 | Página inicial explicativa y navegación | Alta | 5 | 4 | Pendiente |
| HU-029 | Modo claro y modo oscuro | Media | 3 | 4 | Pendiente |
| HU-030 | Listados con paginación, filtrado y ordenamiento | Media | 5 | 4 | Pendiente |
| HU-031 | API REST versionada y documentada | Alta | 5 | 4 | Pendiente |
| HU-032 | Errores de la API comprensibles y seguros | Alta | 3 | 4 | Pendiente |
| HU-033 | Ejecutar el sistema con Docker Compose | Alta | 3 | 4 | Pendiente |
| HU-034 | Desplegar el sistema en Kubernetes | Alta | 5 | 4 | Pendiente |
| HU-035 | Verificar el flujo completo desde el navegador | Alta | 5 | 4 | Pendiente |

**Total:** 127 puntos distribuidos en cuatro iteraciones — 29, 29, 35 y 34 respectivamente.
El desbalance de las dos últimas está identificado como riesgo en
[plan-xp.md](plan-xp.md#5-riesgos-del-plan).

---

## Iteración 1 — Fundación y reglas de dominio

### HU-001 · Estructura del proyecto e integración continua

**Como** programadora del equipo
**quiero** partir de una solución con las capas separadas y una verificación automática en cada integración
**para** que ningún cambio que rompa la compilación, el formato o las pruebas llegue a la rama principal.

- **Prioridad:** Alta · **Estimación:** 3 puntos · **Iteración:** 1

**Criterios de aceptación**

1. La solución contiene los cinco proyectos de `/src` y los tres de `/tests` con la
   estructura acordada.
2. `Licitaciones.Domain` no referencia ningún proyecto ni paquete de infraestructura, y
   existe una prueba automatizada que lo verifica.
3. `Licitaciones.Application` no referencia Entity Framework Core.
4. `dotnet build` termina sin errores y sin advertencias.
5. `dotnet format --verify-no-changes` no reporta diferencias.
6. El flujo de integración continua se ejecuta en cada envío a la rama principal y en cada
   solicitud de integración, y falla si cualquiera de los pasos anteriores falla.

**Trazabilidad**
- Pruebas: `DireccionDependenciasTests`
- Commits: `e98ac44`, `3044439`, `c15f872`, `8fae287`
- Documentación: [arquitectura-general.md](arquitectura-general.md)

---

### HU-002 · Ciclo de estados de una licitación

**Como** encargado de compras
**quiero** que una licitación solo avance por transiciones válidas
**para** que nadie pueda devolver a preparación un proceso ya publicado ni reabrir uno cerrado.

- **Prioridad:** Alta · **Estimación:** 5 puntos · **Iteración:** 1

**Criterios de aceptación**

1. Una licitación en Borrador puede pasar a Publicada si tiene los datos completos, un
   presupuesto mayor que cero y una fecha de cierre futura.
2. Una licitación en Borrador puede pasar a Cerrada registrando el motivo de la
   cancelación.
3. Una licitación Publicada puede pasar a Cerrada.
4. Intentar pasar de Publicada a Borrador se rechaza con el mensaje
   "Transición de estado no permitida."
5. Intentar pasar de Cerrada a cualquier otro estado se rechaza con el mismo mensaje.
6. Las transiciones se resuelven en un único punto del dominio, no mediante condicionales
   repartidos por los controladores.

**Trazabilidad**
- Pruebas: `MaquinaEstadosLicitacionTests` (12 casos)
- Commits: `7cce7cd` (rojo), `865617f` (verde), `097a14c` (refactorización)
- Documentación: [modulos/licitaciones.md](modulos/licitaciones.md)

---

### HU-003 · Cierre funcional al alcanzarse la fecha

**Como** encargado de compras
**quiero** que una licitación deje de admitir ofertas en cuanto llega su fecha de cierre
**para** que no dependa de que alguien actualice manualmente el estado.

- **Prioridad:** Alta · **Estimación:** 3 puntos · **Iteración:** 1

**Criterios de aceptación**

1. Una licitación cuyo estado es Publicada pero cuya fecha de cierre ya pasó se considera
   cerrada para todos los efectos.
2. El momento exacto de la fecha de cierre ya cuenta como cerrada: si la hora actual es
   igual a la fecha de cierre, no se admiten ofertas.
3. Toda validación sobre ofertas consulta esta condición y no únicamente el campo de
   estado.
4. La hora del sistema se obtiene de un servicio inyectable, de modo que las pruebas
   pueden fijar un momento determinado sin depender del reloj de la máquina.

**Trazabilidad**
- Pruebas: `CierreFuncionalLicitacionTests` (6 casos)
- Commits: `844cf9b` (rojo), `354fddf` (verde)
- Documentación: [modulos/licitaciones.md](modulos/licitaciones.md)

---

### HU-004 · Código de licitación único

**Como** encargado de compras
**quiero** que dos licitaciones no puedan compartir el mismo código
**para** poder identificar cada proceso sin ambigüedad.

- **Prioridad:** Alta · **Estimación:** 3 puntos · **Iteración:** 1

**Criterios de aceptación**

1. Los códigos `LIC-001`, `lic-001` y `  LIC-001  ` se consideran el mismo código.
2. Al intentar registrar un código ya existente se muestra
   "Ya existe una licitación con ese código."
3. El código original se conserva tal como lo escribió la persona usuaria; la comparación
   usa una versión normalizada.

**Trazabilidad**
- Pruebas: `NormalizadorCodigoTests` (10 casos)
- Commits: `220ad5c` (rojo), `c56c0e8` (verde), `3063fa8` (refactorización)
- Documentación: [modulos/licitaciones.md](modulos/licitaciones.md)

---

### HU-005 · Nombre de proveedor único

**Como** encargado de compras
**quiero** que el sistema detecte que dos nombres escritos distinto son el mismo proveedor
**para** no terminar con la misma empresa registrada varias veces.

- **Prioridad:** Alta · **Estimación:** 5 puntos · **Iteración:** 1

**Criterios de aceptación**

1. `Empresa Central`, `empresa central`, `EMPRESA CENTRAL` y `"  Empresa   Central  "`
   se consideran el mismo proveedor.
2. Al intentar registrar un nombre equivalente a uno existente se muestra
   "Ya existe un proveedor con ese nombre."
3. La comparación elimina espacios laterales, reduce espacios repetidos, normaliza la
   representación Unicode y no distingue mayúsculas de minúsculas.
4. El nombre se conserva tal como lo escribió la persona usuaria.

**Trazabilidad**
- Pruebas: `NormalizadorNombreProveedorTests` (12 casos)
- Commits: `46c77ea` (rojo), `e9ba341` (verde)
- Documentación: [modulos/proveedores.md](modulos/proveedores.md)

---

### HU-006 · Caracteres permitidos en el nombre del proveedor

**Como** encargado de compras
**quiero** que el nombre de un proveedor solo admita caracteres razonables
**para** evitar registros con símbolos que ensucien los reportes.

- **Prioridad:** Alta · **Estimación:** 2 puntos · **Iteración:** 1

**Criterios de aceptación**

1. Se aceptan letras (incluidas acentuadas y la eñe), números, espacios, punto, coma y
   paréntesis. Ejemplos válidos: `Empresa Central S.A.`, `Constructora (CR), Ltda.`,
   `Proveedor 2000`.
2. Se rechazan `Empresa@Central`, `Empresa & Cía`, `Empresa/Central`, `Empresa#1`, la
   cadena vacía y una cadena de solo espacios.
3. El mensaje de rechazo es "El nombre solo admite letras, números, espacios, punto, coma
   y paréntesis."
4. La restricción se aplica tanto en el formulario como en el servidor.

**Trazabilidad**
- Pruebas: `ValidadorNombreProveedorTests` (19 casos)
- Commits: `044e3ce` (rojo), `1eb0536` (verde)
- Documentación: [modulos/proveedores.md](modulos/proveedores.md)

---

### HU-007 · Montos siempre mayores que cero

**Como** encargado de compras
**quiero** que ningún monto pueda ser cero ni negativo
**para** que no se registren procesos ni ofertas sin sentido económico.

- **Prioridad:** Alta · **Estimación:** 2 puntos · **Iteración:** 1

**Criterios de aceptación**

1. Un presupuesto de `0` se rechaza con "El presupuesto debe ser mayor que cero."
2. Un presupuesto negativo se rechaza con el mismo mensaje.
3. Un presupuesto de `0,01` se acepta.
4. Un monto ofertado de `0` o negativo se rechaza con "El monto ofertado debe ser mayor
   que cero."
5. Un tipo de cambio de `0` o negativo se rechaza.
6. Los montos conservan exactamente dos decimales, sin pérdida de precisión.

**Trazabilidad**
- Pruebas: `MontoCRCTests` (16 casos)
- Commits: `5b8fbd3` (rojo), `d1328c5` (verde), `3063fa8` (refactorización)
- Documentación: [modulos/ofertas.md](modulos/ofertas.md)

---

### HU-008 · Selección de la mejor oferta

**Como** encargado de compras
**quiero** que el sistema determine cuál es la mejor oferta de una licitación
**para** no depender de una comparación manual.

- **Prioridad:** Alta · **Estimación:** 3 puntos · **Iteración:** 1

**Criterios de aceptación**

1. La mejor oferta es la de menor monto en colones entre las ofertas válidas.
2. Si dos ofertas tienen el mismo monto, gana la registrada primero.
3. Si dos ofertas tienen el mismo monto y la misma fecha de registro, el desempate es
   determinista y siempre devuelve el mismo resultado.
4. Si la licitación no tiene ofertas válidas, el sistema lo indica en lugar de fallar.

**Trazabilidad**
- Pruebas: `EvaluadorMejorOfertaTests` (7 casos)
- Commits: `7a51e81` (rojo), `f4a0f90` (verde)
- Documentación: [modulos/ofertas.md](modulos/ofertas.md)

---

### HU-009 · Clasificación del ahorro obtenido

**Como** aprobador
**quiero** ver una etiqueta que resuma qué tan conveniente es la mejor oferta
**para** decidir rápidamente sin calcular porcentajes.

- **Prioridad:** Alta · **Estimación:** 3 puntos · **Iteración:** 1

**Criterios de aceptación**

1. El ahorro se calcula como la diferencia entre presupuesto y mejor oferta, dividida
   entre el presupuesto, expresada en porcentaje.
2. Sin ofertas válidas, la etiqueta es exactamente **"Sin ofertas válidas"**.
3. Con un ahorro igual o superior al 10 %, la etiqueta es **"Oferta conveniente"**.
   Un ahorro de exactamente 10 % cae en esta categoría.
4. Con un ahorro mayor que 0 % y menor que 10 %, la etiqueta es **"Oferta aceptable"**.
   Un ahorro de 9,99 % cae en esta categoría.
5. Cuando la oferta es igual al presupuesto, la etiqueta es
   **"Oferta válida sin ahorro"**.

**Trazabilidad**
- Pruebas: `ClasificadorAhorroTests` (11 casos)
- Commits: `131cb81` (rojo), `1fe9435` (verde)
- Documentación: [modulos/ofertas.md](modulos/ofertas.md)

---

## Iteración 2 — Persistencia, proveedores y licitaciones

### HU-010 · Persistencia en PostgreSQL con datos iniciales

**Como** encargado de compras
**quiero** que la información quede guardada de forma permanente
**para** recuperarla después de cerrar o reiniciar el sistema.

- **Prioridad:** Alta · **Estimación:** 5 puntos · **Iteración:** 2

**Criterios de aceptación**

1. El esquema se crea aplicando migraciones versionadas sobre una base vacía.
2. Los montos se almacenan con dos decimales exactos y se recuperan sin pérdida de
   precisión.
3. Las fechas se almacenan con zona horaria y se recuperan correctamente en UTC.
4. Al instalar el sistema por primera vez existen tres niveles de aprobación y un tipo de
   cambio activo, sin necesidad de cargarlos a mano.
5. Los índices únicos impiden guardar códigos de licitación repetidos, nombres de
   proveedor equivalentes y dos ofertas del mismo proveedor para la misma licitación.
6. Las pruebas de estas condiciones se ejecutan contra PostgreSQL real en contenedor.

**Trazabilidad**
- Pruebas: `PersistenciaTests` (8 casos), `RestriccionesTests` (6 casos)
- Commits: `ea8bc60` (rojo), `99304a1` (verde)
- Documentación: [modelo-datos.md](modelo-datos.md), [modulos/persistencia.md](modulos/persistencia.md)

---

### HU-011 · Registro automático de fechas y borrado lógico

**Como** aprobador
**quiero** saber cuándo se creó y se modificó cada registro, y que nada se borre de verdad
**para** poder auditar el proceso más adelante.

- **Prioridad:** Media · **Estimación:** 3 puntos · **Iteración:** 2

**Criterios de aceptación**

1. Al crear un registro se guarda automáticamente su fecha de creación.
2. Al modificarlo se actualiza automáticamente su fecha de modificación.
3. Eliminar una licitación o un proveedor los marca como eliminados con la fecha del
   borrado, en lugar de suprimirlos.
4. Los registros marcados como eliminados no aparecen en los listados ni en las consultas
   ordinarias.
5. Las ofertas asociadas a un registro eliminado lógicamente se conservan.

**Trazabilidad**
- Pruebas: `AuditoriaYBorradoLogicoTests` (4 casos)
- Commits: `0b2c931` (rojo), `d1220d1` (verde)
- Documentación: [modulos/persistencia.md](modulos/persistencia.md)

---

### HU-012 · Protección ante ediciones simultáneas

**Como** encargado de compras
**quiero** que el sistema me avise si alguien modificó un registro mientras yo lo editaba
**para** no sobrescribir su trabajo sin darme cuenta.

- **Prioridad:** Media · **Estimación:** 3 puntos · **Iteración:** 2

**Criterios de aceptación**

1. Si dos personas editan el mismo registro, la segunda en guardar recibe un aviso de que
   el registro cambió y se le muestran los datos actualizados.
2. El aviso es comprensible y no expone detalles técnicos de la base de datos.
3. En la API, esta situación devuelve el código 409.

**Trazabilidad**
- Pruebas: `ConcurrenciaYTransaccionesTests` (3 casos)
- Commits: `0b2c931` (rojo), `d1220d1` (verde)
- Documentación: [modulos/persistencia.md](modulos/persistencia.md)

---

### HU-013 · Administrar proveedores

**Como** encargado de compras
**quiero** registrar, consultar, editar y dar de baja proveedores
**para** mantener actualizado el catálogo de empresas que pueden ofertar.

- **Prioridad:** Alta · **Estimación:** 5 puntos · **Iteración:** 2

**Criterios de aceptación**

1. Puedo crear un proveedor indicando su nombre, y el sistema aplica las reglas de
   unicidad y de caracteres permitidos.
2. Puedo ver la lista de proveedores y el detalle de cada uno.
3. Puedo editar el nombre de un proveedor, con las mismas validaciones.
4. Puedo dar de baja un proveedor, y el sistema me pide confirmación antes de hacerlo.
5. Si el proveedor tiene ofertas registradas, no se elimina físicamente: se marca como
   eliminado y sus ofertas se conservan.
6. Las mismas operaciones están disponibles por la API.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/proveedores.md](modulos/proveedores.md)

---

### HU-014 · Consultar las ofertas de un proveedor

**Como** encargado de compras
**quiero** ver todas las ofertas que ha presentado un proveedor
**para** conocer su historial de participación.

- **Prioridad:** Media · **Estimación:** 2 puntos · **Iteración:** 2

**Criterios de aceptación**

1. Desde el detalle de un proveedor puedo abrir la lista de sus ofertas.
2. Cada oferta muestra la licitación a la que pertenece, el monto y la fecha de registro.
3. Si el proveedor no ha presentado ofertas, se muestra un mensaje informativo en lugar de
   una tabla vacía.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/proveedores.md](modulos/proveedores.md)

---

### HU-015 · Administrar licitaciones

**Como** encargado de compras
**quiero** crear, consultar, editar y dar de baja licitaciones
**para** llevar el control de los procesos de compra.

- **Prioridad:** Alta · **Estimación:** 5 puntos · **Iteración:** 2

**Criterios de aceptación**

1. Puedo crear una licitación indicando código, título, presupuesto estimado y fecha y
   hora de cierre.
2. La fecha y hora de cierre se selecciona mediante un control de calendario, no
   escribiendo texto libre.
3. El identificador lo genera el sistema; no aparece en el formulario ni puedo editarlo.
4. Puedo ver la lista de licitaciones y el detalle de cada una, con su estado.
5. Puedo editar una licitación respetando las reglas de código único y presupuesto
   positivo.
6. Puedo dar de baja una licitación con confirmación previa; si tiene ofertas, el borrado
   es lógico.
7. Las mismas operaciones están disponibles por la API.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/licitaciones.md](modulos/licitaciones.md)

---

### HU-016 · Cambiar el estado de una licitación

**Como** encargado de compras
**quiero** publicar y cerrar licitaciones desde la aplicación
**para** controlar cuándo se reciben ofertas.

- **Prioridad:** Alta · **Estimación:** 3 puntos · **Iteración:** 2

**Criterios de aceptación**

1. Desde el detalle de una licitación puedo ejecutar las transiciones permitidas para su
   estado actual.
2. Las transiciones no permitidas no se ofrecen en la interfaz y, si se intentan por la
   API, se rechazan con el código 409.
3. Publicar una licitación con fecha de cierre ya pasada se rechaza.
4. El cambio de estado pide confirmación antes de ejecutarse.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/licitaciones.md](modulos/licitaciones.md)

---

### HU-017 · No reducir el presupuesto bajo una oferta existente

**Como** encargado de compras
**quiero** que el sistema impida bajar el presupuesto por debajo de una oferta ya recibida
**para** no invalidar retroactivamente ofertas que eran correctas cuando se presentaron.

- **Prioridad:** Alta · **Estimación:** 3 puntos · **Iteración:** 2

**Criterios de aceptación**

1. Si la oferta más alta registrada es de 800 000,00 CRC, no puedo bajar el presupuesto a
   799 999,99 CRC.
2. Puedo bajarlo hasta exactamente 800 000,00 CRC.
3. El mensaje de rechazo indica cuál es la oferta más alta registrada.
4. La restricción no aplica si la licitación no tiene ofertas.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/licitaciones.md](modulos/licitaciones.md)

---

## Iteración 3 — Ofertas, aprobación y moneda

### HU-018 · Registrar una oferta válida

**Como** encargado de compras
**quiero** registrar la oferta económica que presenta un proveedor
**para** dejar constancia de su propuesta.

- **Prioridad:** Alta · **Estimación:** 5 puntos · **Iteración:** 3

**Criterios de aceptación**

1. Puedo registrar una oferta indicando la licitación, el proveedor y el monto en colones.
2. La oferta solo se acepta si la licitación está publicada y aún no ha cerrado.
3. Una oferta sobre una licitación en Borrador se rechaza con "La licitación no está
   publicada."
4. Queda registrada la fecha y hora exacta en que se recibió la oferta.
5. La misma operación está disponible por la API y responde con el código 201 y la
   ubicación del recurso creado.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/ofertas.md](modulos/ofertas.md)

---

### HU-019 · Rechazar una oferta superior al presupuesto

**Como** encargado de compras
**quiero** que el sistema rechace ofertas que superen el presupuesto estimado
**para** garantizar que ninguna adjudicación exceda lo autorizado.

- **Prioridad:** Alta · **Estimación:** 2 puntos · **Iteración:** 3

**Criterios de aceptación**

1. Con un presupuesto de 1 000 000,00 CRC, una oferta de 1 000 000,01 CRC se rechaza con
   "La oferta no puede superar el presupuesto de la licitación."
2. Con el mismo presupuesto, una oferta de 1 000 000,00 CRC se acepta.
3. Con el mismo presupuesto, una oferta de 999 999,99 CRC se acepta.
4. La API devuelve el código 422 con el código de error `OFERTA_SUPERA_PRESUPUESTO`.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/ofertas.md](modulos/ofertas.md)

---

### HU-020 · Impedir una segunda oferta del mismo proveedor

**Como** encargado de compras
**quiero** que cada proveedor tenga a lo sumo una oferta por licitación
**para** que la comparación sea clara y no haya duda de cuál propuesta cuenta.

- **Prioridad:** Alta · **Estimación:** 3 puntos · **Iteración:** 3

**Criterios de aceptación**

1. Al intentar registrar una segunda oferta del mismo proveedor para la misma licitación
   se muestra "Este proveedor ya registró una oferta para esta licitación."
2. La restricción se verifica en el formulario, en el servidor y en la base de datos.
3. Si dos solicitudes simultáneas intentan crear la oferta, solo una se guarda y la otra
   recibe el mensaje controlado, nunca un error técnico.
4. El mismo proveedor sí puede ofertar en licitaciones distintas.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/ofertas.md](modulos/ofertas.md)

---

### HU-021 · Impedir ofertas fuera del período de recepción

**Como** encargado de compras
**quiero** que no se acepten ofertas después de la fecha de cierre
**para** que el plazo se respete sin excepciones.

- **Prioridad:** Alta · **Estimación:** 3 puntos · **Iteración:** 3

**Criterios de aceptación**

1. Antes de la fecha de cierre, la oferta se acepta.
2. En el momento exacto de la fecha de cierre, la oferta se rechaza.
3. Después de la fecha de cierre, la oferta se rechaza.
4. El mensaje es "La licitación ya cerró; no se admiten más ofertas."
5. La comparación se hace en UTC, con independencia de la zona horaria del navegador.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/ofertas.md](modulos/ofertas.md)

---

### HU-022 · Preservar las ofertas de licitaciones cerradas

**Como** aprobador
**quiero** que las ofertas de un proceso cerrado no puedan modificarse ni borrarse
**para** que sirvan como evidencia de lo ocurrido.

- **Prioridad:** Alta · **Estimación:** 3 puntos · **Iteración:** 3

**Criterios de aceptación**

1. Una oferta de una licitación cerrada no puede editarse; el mensaje es "Las ofertas de
   licitaciones cerradas no pueden modificarse."
2. Una oferta de una licitación cerrada no puede eliminarse.
3. La restricción aplica igual si la licitación está cerrada por su estado o porque llegó
   su fecha de cierre.
4. Las acciones de editar y eliminar no se ofrecen en la interfaz para esas ofertas.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/ofertas.md](modulos/ofertas.md)

---

### HU-023 · Consultar la mejor oferta con su clasificación

**Como** aprobador
**quiero** ver en un solo lugar cuál es la mejor oferta de una licitación y qué tan conveniente es
**para** tomar la decisión sin armar un cuadro comparativo.

- **Prioridad:** Alta · **Estimación:** 3 puntos · **Iteración:** 3

**Criterios de aceptación**

1. Desde el detalle de una licitación veo la mejor oferta, el proveedor que la presentó,
   el porcentaje de ahorro y la clasificación.
2. Si no hay ofertas válidas, veo la etiqueta "Sin ofertas válidas" y no un error.
3. La misma información está disponible por la API.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/licitaciones.md](modulos/licitaciones.md)

---

### HU-024 · Administrar niveles de aprobación sin traslape

**Como** cliente
**quiero** definir yo los rangos de monto y su aprobador
**para** poder ajustar la política de autorización sin pedir un cambio en el programa.

- **Prioridad:** Alta · **Estimación:** 5 puntos · **Iteración:** 3

**Criterios de aceptación**

1. Puedo crear, consultar, editar y eliminar niveles de aprobación indicando monto mínimo,
   monto máximo opcional y aprobador.
2. Al crear un rango que se traslapa con otro existente, se rechaza con "El rango se
   traslapa con un nivel existente."
3. Al crear un segundo rango sin monto máximo, se rechaza con "Ya existe un nivel sin
   monto máximo."
4. El monto mínimo debe ser mayor que cero y el máximo, cuando existe, no puede ser menor
   que el mínimo.
5. La verificación de traslape considera todos los demás rangos y se ejecuta dentro de una
   transacción.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/niveles-aprobacion.md](modulos/niveles-aprobacion.md)

---

### HU-025 · Conocer quién debe aprobar un monto

**Como** aprobador
**quiero** que el sistema me diga a quién le corresponde autorizar la mejor oferta
**para** dirigir el trámite a la instancia correcta.

- **Prioridad:** Alta · **Estimación:** 3 puntos · **Iteración:** 3

**Criterios de aceptación**

1. Un monto de 0,01 CRC corresponde a "Encargado de área".
2. Un monto de 999 999,99 CRC corresponde a "Encargado de área".
3. Un monto de 1 000 000,00 CRC corresponde a "Gerencia".
4. Un monto de 9 999 999,99 CRC corresponde a "Gerencia".
5. Un monto de 10 000 000,00 CRC corresponde a "Junta Directiva".
6. Un monto de 50 000 000,00 CRC corresponde a "Junta Directiva".
7. El aprobador se obtiene consultando la tabla de niveles, no mediante condicionales
   escritos en el código.
8. Si ningún rango aplica al monto, se muestra un mensaje controlado en lugar de fallar.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/niveles-aprobacion.md](modulos/niveles-aprobacion.md)

---

### HU-026 · Administrar el tipo de cambio vigente

**Como** administrador del tipo de cambio
**quiero** registrar tasas de cambio y elegir cuál está vigente
**para** que las conversiones usen siempre la tasa correcta sin depender de Internet.

- **Prioridad:** Alta · **Estimación:** 5 puntos · **Iteración:** 3

**Criterios de aceptación**

1. Puedo crear, consultar, editar y eliminar tipos de cambio, indicando la cantidad de
   colones por dólar y su fecha de vigencia.
2. Puedo activar un tipo de cambio, y al hacerlo el que estaba activo deja de estarlo.
3. En ningún momento hay más de un tipo de cambio activo, ni siquiera si la operación de
   activación falla a la mitad.
4. La tasa debe ser mayor que cero.
5. El sistema funciona sin conexión a Internet: nunca consulta un servicio externo de
   tasas.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/tipo-cambio.md](modulos/tipo-cambio.md)

---

### HU-027 · Ver los montos en dólares

**Como** aprobador
**quiero** poder leer los montos en dólares además de en colones
**para** compararlos con referencias internacionales.

- **Prioridad:** Alta · **Estimación:** 3 puntos · **Iteración:** 3

**Criterios de aceptación**

1. El monto en dólares se obtiene dividiendo el monto en colones entre la tasa activa, y
   se muestra con dos decimales.
2. Junto al monto convertido se muestra siempre la tasa utilizada y su fecha de vigencia.
3. Los montos almacenados siguen expresados únicamente en colones: la conversión no
   modifica ningún dato.
4. Los colones se presentan con el formato de Costa Rica, por ejemplo `₡1.250.000,00`.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/tipo-cambio.md](modulos/tipo-cambio.md)

---

## Iteración 4 — Interfaz, API, pruebas de aceptación y despliegue

### HU-028 · Página inicial explicativa y navegación

**Como** persona que entra por primera vez al sistema
**quiero** entender qué hace la aplicación y cómo llegar a cada sección
**para** poder usarla sin que nadie me la explique.

- **Prioridad:** Alta · **Estimación:** 5 puntos · **Iteración:** 4

**Criterios de aceptación**

1. La página inicial explica con texto real el propósito del sistema, el flujo Borrador →
   Publicada → Cerrada, qué son las ofertas, cómo se determina la mejor oferta, qué es el
   nivel de aprobación y cómo funciona la conversión de moneda.
2. Incluye un diagrama del flujo de la licitación.
3. El menú da acceso a inicio, licitaciones, proveedores, ofertas, niveles de aprobación,
   tipo de cambio y la documentación interactiva de la API.
4. La interfaz se adapta correctamente a pantallas de computadora y de teléfono.
5. Todos los recursos visuales se cargan desde el propio servidor: la página se ve
   correctamente sin acceso a Internet.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/interfaz-web.md](modulos/interfaz-web.md)

---

### HU-029 · Modo claro y modo oscuro

**Como** persona usuaria del sistema
**quiero** elegir entre un tema claro y uno oscuro
**para** trabajar cómodamente según la iluminación del lugar.

- **Prioridad:** Media · **Estimación:** 3 puntos · **Iteración:** 4

**Criterios de aceptación**

1. Hay un control visible en la barra de navegación para alternar el tema.
2. La preferencia se conserva al recargar la página y al navegar entre secciones.
3. La primera vez, el sistema respeta la preferencia configurada en el sistema operativo.
4. La página no parpadea con el tema equivocado al cargar.
5. El contraste del texto es suficiente en ambos temas.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/interfaz-web.md](modulos/interfaz-web.md)

---

### HU-030 · Listados con paginación, filtrado y ordenamiento

**Como** encargado de compras
**quiero** buscar, ordenar y recorrer por páginas los listados
**para** encontrar un registro sin revisar toda la tabla.

- **Prioridad:** Media · **Estimación:** 5 puntos · **Iteración:** 4

**Criterios de aceptación**

1. Los cinco listados tienen paginación, un campo de búsqueda y encabezados que ordenan al
   hacer clic.
2. La columna por la que se está ordenando se distingue visualmente.
3. Las ofertas pueden filtrarse por licitación y por proveedor.
4. Las licitaciones pueden filtrarse por estado.
5. Cuando no hay resultados se muestra un mensaje informativo, no una tabla vacía.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/interfaz-web.md](modulos/interfaz-web.md)

---

### HU-031 · API REST versionada y documentada

**Como** persona que integra otro sistema
**quiero** una interfaz de programación estable y documentada
**para** automatizar las operaciones sin usar el navegador.

- **Prioridad:** Alta · **Estimación:** 5 puntos · **Iteración:** 4

**Criterios de aceptación**

1. Todas las rutas están bajo `api/v1`.
2. Las respuestas usan objetos de transferencia; ninguna entidad de persistencia se expone
   directamente.
3. Existe documentación interactiva accesible desde el menú de la aplicación.
4. Cada operación devuelve el código correcto: 200 para consultas, 201 con la ubicación
   del recurso para creaciones, 204 para eliminaciones.
5. Los listados aceptan página, tamaño, orden y búsqueda, y responden indicando el total
   de elementos y de páginas.
6. Existe una colección de solicitudes reproducible dentro de `/docs` que recorre el flujo
   completo.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [api.md](api.md), [modulos/api-rest.md](modulos/api-rest.md)

---

### HU-032 · Errores de la API comprensibles y seguros

**Como** persona que integra otro sistema
**quiero** que los errores me digan qué pasó de forma clara
**para** corregir mi solicitud sin adivinar, y sin que el sistema me revele su funcionamiento interno.

- **Prioridad:** Alta · **Estimación:** 3 puntos · **Iteración:** 4

**Criterios de aceptación**

1. Todo error devuelve un cuerpo con título, estado, descripción segura, un código propio
   del dominio y un identificador de correlación.
2. Una solicitud mal formada devuelve 400; un recurso inexistente, 404; un conflicto con
   el estado actual del sistema, 409; una violación de regla de negocio sobre datos
   correctos, 422.
3. Ningún error expone trazas de pila, rutas de archivos, consultas ni credenciales.
4. Un error no previsto devuelve una respuesta controlada, y el detalle completo queda
   únicamente en el registro del servidor con el mismo identificador de correlación.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [modulos/api-rest.md](modulos/api-rest.md)

---

### HU-033 · Ejecutar el sistema con Docker Compose

**Como** programadora del equipo
**quiero** levantar la aplicación y su base de datos con un solo comando
**para** que cualquiera pueda ejecutar el sistema sin instalar nada más.

- **Prioridad:** Alta · **Estimación:** 3 puntos · **Iteración:** 4

**Criterios de aceptación**

1. `docker compose up --build` levanta la aplicación y PostgreSQL sin pasos manuales
   adicionales.
2. Ambos servicios reportan su estado de salud y la aplicación espera a que la base esté
   lista.
3. Los datos sobreviven a detener y volver a levantar los contenedores.
4. Las credenciales se toman de variables de entorno; el repositorio solo contiene una
   plantilla con valores ficticios.
5. La imagen se construye en varias etapas y la aplicación se ejecuta con un usuario sin
   privilegios.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [docker.md](docker.md)

---

### HU-034 · Desplegar el sistema en Kubernetes

**Como** programadora del equipo
**quiero** desplegar la aplicación en un clúster con almacenamiento persistente
**para** demostrar que el sistema funciona en un entorno orquestado.

- **Prioridad:** Alta · **Estimación:** 5 puntos · **Iteración:** 4

**Criterios de aceptación**

1. Los manifiestos despliegan la aplicación y PostgreSQL en su propio espacio de nombres.
2. La aplicación declara sondas de arranque, de disponibilidad y de vitalidad, y límites
   de procesador y memoria.
3. La configuración no sensible viene de un mapa de configuración y las credenciales de un
   secreto; el repositorio solo versiona un ejemplo con valores ficticios.
4. Las migraciones se aplican de forma controlada una sola vez, no en cada réplica.
5. Al eliminar el pod de la base de datos, este se recrea y los datos siguen ahí.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [kubernetes.md](kubernetes.md)

---

### HU-035 · Verificar el flujo completo desde el navegador

**Como** cliente
**quiero** que el recorrido completo del sistema se verifique automáticamente
**para** confiar en que sigue funcionando después de cada cambio.

- **Prioridad:** Alta · **Estimación:** 5 puntos · **Iteración:** 4

**Criterios de aceptación**

1. Una prueba automatizada recorre en un navegador real: página inicial, cambio de tema,
   registro de proveedor, creación y publicación de licitación, registro de una oferta
   válida, rechazo de una oferta duplicada, de una superior al presupuesto y de una
   vencida, consulta de la mejor oferta con su clasificación y aprobador, y alternancia
   entre colones y dólares.
2. Las pruebas verifican que los mensajes de validación aparecen junto al campo
   correspondiente.
3. Las pruebas cubren el recorrido completo de creación, consulta, edición y eliminación
   en los cinco módulos.
4. La cobertura de líneas alcanza al menos 80 % en dominio y aplicación, y 70 % en el
   proyecto completo.

**Trazabilidad**
- Pruebas: pendiente
- Commits: pendiente
- Documentación: [pruebas.md](pruebas.md)
