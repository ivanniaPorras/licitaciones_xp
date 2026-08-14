# Plan de trabajo XP

Volver al [índice de la documentación](README.md).

Este documento recoge el resultado del **Planning Game**: qué se entrega, en qué orden y
bajo qué reglas trabaja el equipo. Se revisa al cierre de cada iteración con la
retroalimentación del cliente.

## 1. Por qué Extreme Programming

El equipo adopta XP como marco rector único. La decisión no es arbitraria: responde a las
características concretas de este proyecto.

| Característica del proyecto | Práctica de XP que la atiende |
|---|---|
| El valor se concentra en reglas de negocio precisas, con casos frontera exactos (ahorro de exactamente 10 %, oferta igual al presupuesto, hora igual a la fecha de cierre). | **Desarrollo dirigido por pruebas.** Cada regla se expresa primero como una prueba que falla; el caso frontera queda fijado antes de existir el código. |
| Los criterios de aceptación provienen de un documento que admite lecturas distintas y se aclaran conversando. | **Cliente disponible y retroalimentación frecuente.** Las ambigüedades se resuelven al cierre de cada iteración, no al final del proyecto. |
| El equipo es de dos personas y ambas deben poder explicar y modificar cualquier parte. | **Programación en parejas y propiedad colectiva del código.** Ninguna parte queda como territorio exclusivo de una integrante. |
| El diseño se irá entendiendo mejor conforme aparezcan las reglas. | **Diseño simple y refactorización continua.** Se implementa lo mínimo que satisface las historias vigentes y se mejora la estructura de forma constante. |
| Cualquier regresión en una regla de negocio es cara de detectar a mano. | **Integración continua.** Cada integración compila, verifica formato y ejecuta todas las pruebas. |
| El plazo es fijo y el alcance está definido de antemano. | **Iteraciones cortas y pequeñas liberaciones.** Al cierre de cada iteración existe software ejecutable y demostrable. |

**Por qué no otro marco ágil.** Los marcos centrados en la gestión del flujo de trabajo
—tableros, roles de coordinación, ceremonias de sincronización— organizan *quién hace qué
y cuándo*, pero no prescriben cómo se escribe el código. Este proyecto se evalúa
principalmente por la corrección de sus reglas de negocio y por la evidencia de pruebas
automatizadas en el historial. XP es el único de los marcos ágiles que impone las
prácticas de ingeniería —pruebas primero, refactorización, integración continua,
programación en parejas, estándares de código— como parte obligatoria del método. Para un
equipo de dos personas, además, la capa de coordinación de esos otros marcos añadiría
formalidad sin resolver ningún problema real: la comunicación directa es suficiente.

## 2. Plan de liberación

Cuatro iteraciones de duración uniforme, cada una cerrada con una pequeña liberación
ejecutable y etiquetada.

| Liberación | Iteración | Qué puede hacer el cliente con ella |
|---|---|---|
| **v0.1.0** | 1 | Verificar, mediante la ejecución de las pruebas, que las reglas de negocio del dominio se comportan como se acordó: transiciones de estado, cierre por fecha, unicidad, montos positivos, mejor oferta y clasificación del ahorro. Todavía no hay interfaz ni base de datos. |
| **v0.2.0** | 2 | Usar la aplicación web sobre PostgreSQL para administrar proveedores y licitaciones, incluido el cambio de estado. Los datos persisten entre reinicios. |
| **v0.3.0** | 3 | Ejecutar el flujo funcional completo: registrar ofertas con todas sus validaciones, consultar la mejor oferta con su clasificación y su aprobador, y ver los montos convertidos a dólares. |
| **v1.0.0** | 4 | Usar el sistema terminado: página inicial explicativa, temas claro y oscuro, listados con paginación y filtros, API documentada, y despliegue reproducible con contenedores y en Kubernetes. |

El orden responde a una regla: **primero las reglas de negocio, después su almacenamiento,
después su presentación.** Una regla mal entendida descubierta en la iteración 1 cuesta
una prueba; descubierta en la iteración 4, cuesta rehacer interfaz y API.

## 3. Plan de cada iteración

La capacidad estimada del equipo es de **30 puntos por iteración**. Es una estimación
inicial: se corregirá con la velocidad observada al cierre de la primera iteración.

### Iteración 1 — Fundación y reglas de dominio · 29 puntos

| ID | Historia | Puntos |
|---|---|---|
| HU-001 | Estructura del proyecto e integración continua | 3 |
| HU-002 | Ciclo de estados de una licitación | 5 |
| HU-003 | Cierre funcional al alcanzarse la fecha | 3 |
| HU-004 | Código de licitación único | 3 |
| HU-005 | Nombre de proveedor único | 5 |
| HU-006 | Caracteres permitidos en el nombre del proveedor | 2 |
| HU-007 | Montos siempre mayores que cero | 2 |
| HU-008 | Selección de la mejor oferta | 3 |
| HU-009 | Clasificación del ahorro obtenido | 3 |

Objetivo: la capa de dominio queda completa y con cobertura alta, sin ninguna dependencia
de infraestructura.

### Iteración 2 — Persistencia, proveedores y licitaciones · 29 puntos

| ID | Historia | Puntos |
|---|---|---|
| HU-010 | Persistencia en PostgreSQL con datos iniciales | 5 |
| HU-011 | Registro automático de fechas y borrado lógico | 3 |
| HU-012 | Protección ante ediciones simultáneas | 3 |
| HU-013 | Administrar proveedores | 5 |
| HU-014 | Consultar las ofertas de un proveedor | 2 |
| HU-015 | Administrar licitaciones | 5 |
| HU-016 | Cambiar el estado de una licitación | 3 |
| HU-017 | No reducir el presupuesto bajo una oferta existente | 3 |

Objetivo: dos módulos funcionando de extremo a extremo sobre PostgreSQL real.

### Iteración 3 — Ofertas, aprobación y moneda · 35 puntos

| ID | Historia | Puntos |
|---|---|---|
| HU-018 | Registrar una oferta válida | 5 |
| HU-019 | Rechazar una oferta superior al presupuesto | 2 |
| HU-020 | Impedir una segunda oferta del mismo proveedor | 3 |
| HU-021 | Impedir ofertas fuera del período de recepción | 3 |
| HU-022 | Preservar las ofertas de licitaciones cerradas | 3 |
| HU-023 | Consultar la mejor oferta con su clasificación | 3 |
| HU-024 | Administrar niveles de aprobación sin traslape | 5 |
| HU-025 | Conocer quién debe aprobar un monto | 3 |
| HU-026 | Administrar el tipo de cambio vigente | 5 |
| HU-027 | Ver los montos en dólares | 3 |

Objetivo: flujo funcional mínimo completo. Es la iteración con más reglas de negocio y la
que concentra el mayor peso de la evaluación.

### Iteración 4 — Interfaz, API, pruebas de aceptación y despliegue · 34 puntos

| ID | Historia | Puntos |
|---|---|---|
| HU-028 | Página inicial explicativa y navegación | 5 |
| HU-029 | Modo claro y modo oscuro | 3 |
| HU-030 | Listados con paginación, filtrado y ordenamiento | 5 |
| HU-031 | API REST versionada y documentada | 5 |
| HU-032 | Errores de la API comprensibles y seguros | 3 |
| HU-033 | Ejecutar el sistema con Docker Compose | 3 |
| HU-034 | Desplegar el sistema en Kubernetes | 5 |
| HU-035 | Verificar el flujo completo desde el navegador | 5 |

Objetivo: sistema terminado, desplegable y documentado.

## 4. Reglas de trabajo del equipo

### 4.1 Definición de terminado

Una historia está terminada cuando **todo** lo siguiente es cierto:

1. Existen pruebas automatizadas que verifican cada uno de sus criterios de aceptación.
2. El historial muestra el ciclo prueba que falla → implementación mínima →
   refactorización, en commits separados.
3. `dotnet build` termina sin errores ni advertencias.
4. `dotnet format --verify-no-changes` no reporta diferencias.
5. `dotnet test` pasa completo.
6. La integración continua está en verde sobre la rama principal.
7. El documento del módulo correspondiente en `/docs/modulos/` está actualizado.
8. La tabla de trazabilidad de la historia enlaza sus pruebas y sus commits.

Una historia no se marca como terminada "a falta de la documentación". La documentación es
parte del trabajo, no un anexo posterior.

### 4.2 Estándares de código

- Las reglas de formato y nomenclatura viven en `.editorconfig` y las verifica la
  integración continua. No se discuten en revisión: si el comando pasa, el formato es
  correcto.
- Las advertencias del compilador se tratan como errores.
- Los nombres del dominio se escriben en español, igual que el lenguaje del cliente:
  `MejorOferta`, `EstaCerradaFuncionalmente`, `NivelAprobacion`.
- Los comentarios se reservan para reglas cuya razón no es evidente al leer el código
  —normalización Unicode, criterio de desempate, comparación en UTC—. No se comentan
  operaciones obvias.
- Los controladores solo orquestan. Si una acción supera unas quince líneas de lógica, esa
  lógica se traslada a un servicio de aplicación.
- Ninguna clase de dominio o de aplicación consulta el reloj del sistema directamente.

### 4.3 Política de integración

- La rama principal es `main` y siempre debe estar en verde.
- Las ramas de trabajo viven **horas, no días**, y se nombran por la historia que
  atienden: `feature/HU-019-oferta-supera-presupuesto`.
- Toda integración a `main` pasa por una solicitud de integración revisada por la otra
  integrante. La revisión es el mecanismo que sustituye a la programación en parejas
  cuando se trabaja por separado.
- Se integra al menos una vez al día. Una rama que lleva más de un día abierta se divide o
  se integra parcialmente.
- Si la integración continua falla, arreglarla tiene prioridad sobre cualquier otro
  trabajo.

### 4.4 Cadencia de refactorización

- La refactorización ocurre **dentro** del ciclo de cada historia, como tercer paso, no en
  una fase aparte al final del proyecto.
- Además, cada iteración incluye al menos una sesión de refactorización conjunta sobre
  código escrito por la otra integrante.
- Se refactoriza solo con las pruebas en verde, y las pruebas no se modifican durante la
  refactorización: si hay que cambiarlas, no es una refactorización sino un cambio de
  comportamiento.

### 4.5 Programación en parejas y rotación de roles

Ambas integrantes participan en las cuatro iteraciones. El reparto define quién **lidera**
cada bloque, no quién es la única que lo toca.

| Momento | Duración | Qué se hace |
|---|---|---|
| Sesión de pareja 1 | 90 min | Ambas sobre el mismo código, rotando conductora y copiloto cada 25 minutos. Tema: la regla de negocio más difícil de la iteración. |
| Sesión de pareja 2 | 90 min | Refactorización conjunta del código escrito por la otra. |
| Traspaso de cierre | 45 min | Quien construyó explica; la otra modifica algo en vivo y lo integra. |
| Revisión cruzada | continua | Toda solicitud de integración la revisa la otra integrante. |

Cada sesión se registra en [bitacora-xp.md](bitacora-xp.md) con fecha, duración y quién
condujo. Los commits producidos en pareja llevan la coautoría de ambas.

### 4.6 Ritmo sostenible

El trabajo se distribuye a lo largo de las semanas de cada iteración, con commits
frecuentes y pequeños. Un historial concentrado en los últimos días contradice la práctica
y es visible en el propio repositorio. La verificación se hace con:

```bash
git log --pretty=format:'%ad %an' --date=short | sort | uniq -c
git shortlog -sn --all
```

La salida se adjunta en la bitácora al cierre de cada iteración.

### 4.7 Retroalimentación del cliente

Al cierre de cada iteración se demuestra la liberación al cliente y se registran en la
bitácora: qué aceptó, qué pidió cambiar y qué historias nuevas surgieron. Las historias
nuevas entran al plan de la siguiente iteración y desplazan a las de menor prioridad si la
velocidad no alcanza.

## 5. Riesgos del plan

| Riesgo | Señal temprana | Qué se hace |
|---|---|---|
| Las iteraciones 3 y 4 están sobrecargadas (35 y 34 puntos frente a una capacidad estimada de 30). | La velocidad observada en las iteraciones 1 y 2 queda por debajo de 29 puntos. | Adelantar a la iteración 2 las historias de menor acoplamiento —HU-026 y HU-027— y bajar de prioridad HU-029 y HU-030, que son las únicas de prioridad media en la iteración 4. |
| La estimación de capacidad es una suposición sin datos previos. | Desviación mayor al 25 % en la primera iteración. | Recalcular la capacidad con la velocidad observada y replanificar las tres iteraciones restantes con el cliente. |
| El trabajo se concentra en una sola cuenta del repositorio. | `git shortlog -sn` muy desbalanceado al revisar la iteración. | Redistribuir historias en la siguiente iteración y reforzar las sesiones de pareja con coautoría. |
| Una de las integrantes no puede explicar el código de la otra. | En el traspaso de cierre, no logra modificar el módulo en vivo. | Repetir la sesión de traspaso hasta lograrlo. Es un criterio de la defensa oral, no un detalle. |
| La documentación se posterga. | Un bloque cerrado sin su archivo de módulo escrito. | La historia no se marca como terminada. La definición de terminado ya lo exige. |
| Las pruebas de integración dependen de Docker en la máquina de quien las ejecute. | Fallos de las pruebas de integración solo en una de las máquinas. | Documentar el requisito en `pruebas.md` y mantener las pruebas unitarias ejecutables sin Docker. |
