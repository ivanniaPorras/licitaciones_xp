# Bitácora XP

Volver al [índice de la documentación](README.md).

Registro por iteración de lo ocurrido realmente: historias comprometidas y terminadas,
velocidad observada, evidencia del ciclo de pruebas, refactorizaciones, liberaciones y
retroalimentación del cliente.

> Las entradas se escriben durante la iteración, no al final del proyecto. Los campos
> marcados como *(por completar)* corresponden a hechos que aún no han ocurrido; se llenan
> cuando ocurren, no antes.

---

## Iteración 1 — Fundación y reglas de dominio

**Fechas:** *(por definir con la fecha de entrega del curso)*

### Historias comprometidas

| ID | Historia | Prioridad | Puntos | Estado |
|----|----------|-----------|--------|--------|
| HU-001 | Estructura del proyecto e integración continua | Alta | 3 | Terminada |
| HU-002 | Ciclo de estados de una licitación | Alta | 5 | Terminada |
| HU-003 | Cierre funcional al alcanzarse la fecha | Alta | 3 | Terminada |
| HU-004 | Código de licitación único | Alta | 3 | Terminada |
| HU-005 | Nombre de proveedor único | Alta | 5 | Terminada |
| HU-006 | Caracteres permitidos en el nombre del proveedor | Alta | 2 | Terminada |
| HU-007 | Montos siempre mayores que cero | Alta | 2 | Terminada |
| HU-008 | Selección de la mejor oferta | Alta | 3 | Terminada |
| HU-009 | Clasificación del ahorro obtenido | Alta | 3 | Terminada |

**Comprometido:** 29 puntos.

### Trabajo realizado hasta el momento

**Entrega 1 — Inicialización y estructura (HU-001, terminada).**

Se creó el repositorio con la estructura completa de la solución: los cinco proyectos de
`/src` y los tres de `/tests`, con las referencias entre proyectos siguiendo la dirección
de dependencias acordada. Se configuraron `.editorconfig`, `.gitattributes` y
`Directory.Build.props` con nulabilidad habilitada, advertencias tratadas como errores y
generación de documentación XML.

Se escribió una prueba de arquitectura, `DireccionDependenciasTests`, que verifica por
reflexión que el ensamblado de dominio no referencia Entity Framework Core, Npgsql ni
ASP.NET Core, y que tampoco referencia la capa de aplicación. Es la primera prueba del
proyecto y falla si alguien agrega una referencia indebida.

Se creó el esqueleto de `/docs` con los 21 archivos obligatorios y su índice navegable, y
el flujo de integración continua que restaura, verifica formato, compila, revisa
dependencias vulnerables y ejecuta las pruebas.

Verificación local al cierre de la entrega:

| Comando | Resultado |
|---|---|
| `dotnet build -c Release` | 0 errores, 0 advertencias |
| `dotnet format --verify-no-changes --severity warn` | sin diferencias |
| `dotnet test` | 3 pruebas superadas |

**Entrega 2 — Historias y planificación XP.**

Se redactaron `vision-alcance.md`, las 35 historias de usuario con sus criterios de
aceptación y `plan-xp.md` con el plan de liberación, el plan de las cuatro iteraciones y
las reglas de trabajo del equipo.

**Entrega 3 — Dominio y modelo de datos (HU-002 a HU-009, terminadas).**

Se construyó la capa de dominio completa siguiendo el ciclo de pruebas en commits
separados. Quedaron implementados: la máquina de estados con sus cinco transiciones, el
cierre funcional por fecha con reloj inyectable, los normalizadores de código de
licitación y de nombre de proveedor, el validador de caracteres admitidos, el objeto de
valor `MontoCRC`, el evaluador de la mejor oferta con su desempate y el clasificador del
ahorro con sus cuatro etiquetas. Se añadieron además las entidades `Proveedor`,
`NivelAprobacion` y `TipoCambio` con las invariantes que cada una protege.

Verificación al cierre de la entrega:

| Comando | Resultado |
|---|---|
| `dotnet build -c Release` | 0 errores, 0 advertencias |
| `dotnet format --verify-no-changes --severity warn` | sin diferencias |
| `dotnet test` | 119 pruebas superadas |
| Cobertura de `Licitaciones.Domain` | 90,2 % de líneas, 81,8 % de ramas |

El compromiso de la entrega era superar el 80 % de cobertura en dominio; se cumple.

**Entrega 4 — Persistencia y migraciones (HU-010 a HU-012, terminadas).**

Se mapeó el modelo a PostgreSQL 16 con una configuración por entidad, la migración inicial
y los datos semilla. Quedaron implementados los cuatro índices únicos —tres de ellos
parciales—, las restricciones de verificación de montos, las claves foráneas restrictivas,
el interceptor de auditoría con borrado lógico, la concurrencia optimista sobre `xmin`, la
traducción de errores de PostgreSQL a mensajes controlados, y los repositorios con su
unidad de trabajo transaccional.

Las pruebas de integración se ejecutan contra **PostgreSQL 16 real en contenedor** mediante
Testcontainers. Ninguna usa base en memoria.

| Comando | Resultado |
|---|---|
| `dotnet build -c Release` | 0 errores, 0 advertencias |
| `dotnet format --verify-no-changes --severity warn` | sin diferencias |
| `dotnet test` unitarias | 119 superadas |
| `dotnet test` integración | 34 superadas |

Estas tres historias pertenecen a la iteración 2 y se adelantaron respecto del plan, que
las situaba después del cierre de la iteración 1.

**Entregas 5 a 8 — Verticales completas de los cuatro módulos (HU-013 a HU-025, terminadas).**

Se construyeron las cuatro verticales que faltaban de la primera mitad: proveedores,
licitaciones, ofertas y niveles de aprobación. Cada una abarca servicio de aplicación con
sus objetos de transferencia, pantallas MVC y endpoints de la API.

La entrega 5 trajo además la infraestructura compartida que usan las tres siguientes y
que la persona B necesitará: `Result<T>`, los códigos de error con su traducción a 404,
409 y 422, la respuesta paginada, el arranque de la aplicación con la conexión por
variable de entorno y la cultura `es-CR`, el versionado `api/v1`, la documentación
OpenAPI y el middleware global de excepciones.

| Comando | Resultado |
|---|---|
| `dotnet build -c Release` | 0 errores, 0 advertencias |
| `dotnet format --verify-no-changes --severity warn` | sin diferencias |
| `dotnet test` unitarias | 204 superadas |
| `dotnet test` integración | 75 superadas |

**Entrega 9 — Tipo de cambio y montos en dólares (HU-026, HU-027, terminadas).**

Se cerró la vertical del tipo de cambio sobre la infraestructura compartida que dejó la
entrega 5. Quedaron implementados el servicio de aplicación con el recorrido completo de
creación, consulta, edición y eliminación, la activación de la tasa vigente dentro de una
transacción, el servicio de conversión con su redondeo alejándose de cero, los endpoints
bajo `api/v1/tipos-cambio` y las pantallas MVC.

La lectura en dólares se resolvió con un alternador en la barra de navegación, apoyado en
los marcadores `data-crc` que ya traían las diez vistas de los otros módulos. La tasa se
publica con un componente de vista y no cargándola en cada controlador, que habría
repetido lo mismo cinco veces.

| Comando | Resultado |
|---|---|
| `dotnet build -c Release` | 0 errores, 0 advertencias |
| `dotnet format --verify-no-changes --severity warn` | sin diferencias |
| `dotnet test` unitarias | 232 superadas |
| `dotnet test` integración | 86 superadas |

Con esta entrega la iteración 3 queda completa: las diez historias comprometidas están
terminadas.

### Decisiones tomadas

| Decisión | Razón |
|---|---|
| Los recursos de Bootstrap, jQuery y jquery-validation se versionan en `wwwroot/lib`. | El sistema debe funcionar sin acceso a Internet. No son artefactos generados por el compilador, sino dependencias necesarias para que la interfaz se renderice. |
| Los finales de línea se normalizan a LF en todo el repositorio. | La integración continua verifica el formato sobre Linux; con finales de línea distintos, el mismo archivo pasaría en una máquina y fallaría en la otra. |
| La entrega 1 se integra desde la rama `estructura-inicial`. | No corresponde a una historia funcional, sino a la preparación del repositorio. A partir de la entrega 3 las ramas se nombran por historia. |
| No se implementa la reapertura de una licitación cerrada. | El enunciado la admite solo con una regla aprobada previamente por la persona docente. Se documenta en el módulo de licitaciones. |
| Un tipo de cambio recién registrado queda fuera de uso hasta que alguien lo active. | Si al guardarlo quedara vigente de inmediato, registrar una tasa cambiaría sin querer todos los montos que ya se están mostrando. |
| Al activar una tasa, la anterior se retira y se confirma antes de marcar la nueva. | El índice único parcial de PostgreSQL se comprueba en cada instrucción y no al cerrar la transacción, así que las dos marcas no pueden coexistir ni por un instante. |
| Eliminar la tasa vigente está permitido y no se bloquea. | El cliente pidió poder eliminar tipos de cambio sin excepciones. Quedarse sin tasa activa no rompe nada: las pantallas siguen en colones y la conversión responde con un mensaje controlado. |

### Velocidad

*(por completar al cierre de la iteración)*

- Planificada: 29 puntos · Observada: —
- Desviación y su causa: —

### Evidencia del ciclo de pruebas

| Historia | Prueba que falla | Implementación mínima | Refactorización |
|----------|------------------|-----------------------|-----------------|
| HU-001 | — | `3044439` | — |
| HU-002 | `7cce7cd` | `865617f` | `097a14c` |
| HU-003 | `844cf9b` | `354fddf` | — |
| HU-004 | `220ad5c` | `c56c0e8` | `3063fa8` |
| HU-005 | `46c77ea` | `e9ba341` | — |
| HU-006 | `044e3ce` | `1eb0536` | — |
| HU-007 | `5b8fbd3` | `d1328c5` | `3063fa8` |
| HU-008 | `7a51e81` | `f4a0f90` | — |
| HU-009 | `131cb81` | `1fe9435` | — |
| HU-010 | `ea8bc60` | `99304a1` | — |
| HU-011 | `0b2c931` | `d1220d1` | — |
| HU-012 | `0b2c931` | `d1220d1` | — |
| HU-013, HU-014 | `706b431` | `b622a85` | — |
| HU-015 a HU-017 | `72f5790` | `f4be3f1` | — |
| HU-018 a HU-022 | `f9c9efe` | `bd80333` | `30b13d7` |
| HU-024, HU-025 | `fe9d765` | `e5c4ad8` | — |
| HU-026 | `c8bcadb` | `baeae97` | — |
| HU-027 | `ac3d6ac` | `551b1ae` | — |

> HU-001 es una historia de preparación del repositorio: la prueba de arquitectura y la
> estructura que verifica se crearon en el mismo commit porque la prueba no puede existir
> sin los proyectos que inspecciona. A partir de HU-002 el ciclo se registra en commits
> separados.
>
> Las historias sin commit de refactorización quedaron con una implementación que no
> admitía mejora sin alterar su comportamiento. Se prefirió no registrar cambios
> cosméticos antes que inflar el historial.

### Refactorizaciones

- **`097a14c`** — La tabla de transiciones de estado devolvía el arreglo interno, de modo
  que quien recibiera el resultado podía convertirlo de vuelta a `EstadoLicitacion[]` y
  modificar la regla en tiempo de ejecución. Se sustituyó por una estructura inmutable.
- **`3063fa8`** — `Licitacion` guardaba el presupuesto como un `decimal` suelto, lo que
  duplicaba conceptualmente las reglas monetarias ya encapsuladas en `MontoCRC`. Se unificó
  y, de paso, la entidad pasó a guardar su código normalizado.

### Defectos encontrados por las pruebas

- **HU-009** — La primera implementación decidía la etiqueta comparando el porcentaje de
  ahorro **redondeado** contra cero. Con un presupuesto de 1 000 000,00 CRC y una oferta de
  999 999,99 CRC, el ahorro redondea a 0,00 % y la oferta se etiquetaba como
  "Oferta válida sin ahorro" pese a ser menor que el presupuesto. La prueba
  `AhorroMinimo_DevuelveOfertaAceptable` lo detectó antes de que el código saliera de la
  rama; la comparación se cambió para hacerse sobre los montos.
- **HU-010** — Las primeras pruebas de persistencia fallaron al guardar una fecha con el
  desplazamiento de Costa Rica: Npgsql solo admite desplazamiento cero en
  `timestamp with time zone`. En vez de convertir a mano en cada llamada, se añadió una
  convención del modelo que lleva toda fecha a tiempo universal en el límite de la
  persistencia. La regla de "comparar siempre en UTC" pasó así a cumplirse sola.

- **Entrega 7** — Las primeras pruebas de los endpoints de ofertas devolvieron 500. La
  consulta que combinaba la oferta con su licitación y su proveedor filtraba y ordenaba
  **sobre el tipo ya proyectado**, algo que Entity Framework Core no puede traducir a SQL,
  y además intentaba evaluar `Monto.Valor` sobre un objeto de valor con conversor. Se
  reescribió para filtrar y ordenar sobre las entidades y combinar después
  (`30b13d7`). Sin las pruebas de endpoint el fallo habría llegado a la interfaz.

### Salvedad sobre el ciclo en la entrega 4

Los repositorios y la unidad de trabajo (`ad58065`) se escribieron **antes** que sus
pruebas, a diferencia del resto del trabajo. Son adaptadores delgados sobre Entity
Framework Core cuyo contrato ya estaba fijado por las interfaces de la capa de aplicación,
así que no había una decisión de diseño que la prueba pudiera guiar. Sus pruebas
—`RepositoriosTests`— se escribieron enseguida y van en el mismo commit. Se registra aquí
porque el historial debe reflejar lo que ocurrió, no lo que convendría que hubiera
ocurrido.

### Sesiones de programación en pareja

| Fecha | Duración | Conductora | Copiloto | Tema |
|---|---|---|---|---|
| *(por completar)* | | | | |

### Integración continua

*(por completar: enlace al primer flujo satisfactorio sobre `main`)*

### Pequeña liberación

*(por completar: tag `v0.1.0` al cierre de la iteración)*

### Retroalimentación del cliente

*(por completar tras la demostración de la liberación)*

### Ajustes para la siguiente iteración

*(por completar)*

---

## Iteración 2 — Persistencia, proveedores y licitaciones

*(sin iniciar)*

---

## Iteración 3 — Ofertas, aprobación y moneda

*(sin iniciar)*

---

## Iteración 4 — Interfaz, API, pruebas de aceptación y despliegue

*(sin iniciar)*
