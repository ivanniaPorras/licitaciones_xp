# Declaración de uso de inteligencia artificial

Volver al [índice de la documentación](README.md).

Declaración exigida por el apartado 16 del enunciado. Recoge qué herramienta se usó, para
qué, en qué partes del proyecto y —lo más importante— qué comprobó cada integrante sobre
lo que la herramienta produjo.

## 1. Herramienta utilizada

Claude Code (Anthropic), integrado en Visual Studio Code.

## 2. Finalidad

La herramienta de inteligencia artificial se utilizó como apoyo durante el desarrollo del
proyecto para orientar la construcción de la solución, facilitar la redacción de
documentación, proponer estructuras iniciales de código y apoyar la implementación de
distintos módulos.

Su uso se realizó de manera asistida y supervisada. Las propuestas generadas no se
consideraron automáticamente correctas, sino que fueron revisadas, probadas y, cuando fue
necesario, corregidas o rechazadas antes de incorporarlas al proyecto.

También se utilizó como apoyo dentro del proceso de desarrollo guiado por pruebas. En las
reglas de negocio se procuró trabajar primero con la prueba, comprobar el fallo esperado y
posteriormente realizar la implementación mínima necesaria para hacerla pasar.

## 3. Módulos asistidos

### Primera mitad — Ivannia Porras Miranda

| Fecha | Módulo o entrega | Qué se solicitó |
|---|---|---|
| 14/08/2026 | Entrega 1 · Bloque A1 | Se solicitó apoyo para definir la estructura inicial de la solución, crear el `.editorconfig`, el `Directory.Build.props`, una integración continua mínima y el esqueleto de la carpeta `/docs`. |
| 14/08/2026 | Entrega 2 · Bloque A2 | Se solicitó apoyo para redactar la visión y el alcance del proyecto, las 35 historias de usuario con sus criterios de aceptación y el plan XP dividido en cuatro iteraciones. |
| 15/08/2026 | Entrega 3 · Bloque A3 | Se solicitó apoyo para construir la capa de dominio siguiendo TDD, incluyendo la máquina de estados, el cierre funcional, los normalizadores, `MontoCRC`, el cálculo de la mejor oferta y la clasificación. |
| 15/08/2026 | Entrega 4 · Bloque A4 | Se solicitó apoyo para desarrollar la capa de persistencia: `DbContext`, configuraciones, migración inicial, semilla de datos, auditoría, control de concurrencia, repositorios y pruebas con Testcontainers. |
| 18/08/2026 | Entregas 5 a 8 · Bloques A5 a A8 | Se solicitó apoyo para desarrollar las verticales de proveedores, licitaciones, ofertas y niveles de aprobación, incluyendo servicios de aplicación, vistas MVC, API y pruebas. |
| 24/08/2026 | Cierre de la entrega | Se solicitó una verificación del proyecto completo contra la rúbrica, y apoyo para completar la arquitectura general, la integración entre módulos, el renombrado de los manifiestos de Kubernetes y la puerta de cobertura en la integración continua. |

### Segunda mitad — Anyelina Chacón Mora

> Las fechas y los módulos están tomados del historial. **La columna de la derecha la
> completa Anyelina**, indicando qué solicitó exactamente en cada caso.

| Fecha | Módulo o entrega | Qué se solicitó |
|---|---|---|
| 20/08/2026 | Entrega 9 · Bloque B1 — Tipo de cambio y conversión monetaria | *(por completar)* |
| 21/08/2026 | Entrega 10 · Bloque B2 — Interfaz web, página inicial, temas claro y oscuro, paginación | *(por completar)* |
| 21/08/2026 | Entrega 11 · Bloque B3 — API transversal, `ProblemDetails`, colección reproducible | *(por completar)* |
| 23/08/2026 | Entrega 12 · Bloque B4 — Pruebas de extremo a extremo con Playwright | *(por completar)* |
| 23/08/2026 | Entrega 13 · Bloque B5 — Imagen multietapa y Docker Compose | *(por completar)* |
| 23/08/2026 | Entrega 14 · Bloque B6 — Manifiestos de Kubernetes e integración continua | *(por completar)* |
| 23/08/2026 | Entrega 15 · Bloque B7 — Documentación y cierre | *(por completar)* |

## 4. Ejemplos relevantes

Uno de los usos más importantes de la herramienta se dio durante el trabajo con TDD. En
lugar de solicitar una solución completa de una sola vez, se trabajó por etapas. Primero
se solicitaba la prueba correspondiente a una regla de negocio, después se ejecutaba para
comprobar que fallara y posteriormente se realizaba la implementación mínima necesaria
para hacerla pasar. Un ejemplo de esto fue la máquina de estados, cuyo proceso quedó
registrado en los commits `7cce7cd` para la etapa roja, `865617f` para la etapa verde y
`097a14c` para el refactor.

También hubo situaciones en las que rechacé o modifiqué propuestas realizadas durante las
sesiones. Por ejemplo, pedí cambiar los mensajes de commit porque inicialmente eran
demasiado estructurados y quería que se vieran más naturales y escritos durante el trabajo.
Además, solicité que la autoría de los commits quedara únicamente a mi nombre, corregí la
estrategia de ramas para que las ramas nuevas salieran desde `main` y decidí no versionar
el archivo `CLAUDE.md`.

No todas las decisiones del proyecto fueron tomadas a partir de recomendaciones de la
herramienta. Algunas decisiones las tomé directamente durante el desarrollo, como utilizar
los nombres de rama `ft/dominio` y `ft/persistencia`, mantener `wwwroot/lib` versionado
después de valorar el riesgo y distribuir el trabajo en varios días en lugar de realizarlo
todo en una sola sesión.

## 5. Validaciones realizadas por las estudiantes

### Primera mitad — Ivannia Porras Miranda

| Fecha | Elemento revisado | Qué se probó | Qué se corrigió o rechazó | Revisado por |
|---|---|---|---|---|
| 15/08/2026 | Clasificación del ahorro | Revisé el comportamiento del cálculo con un presupuesto de 1 000 000 y una oferta de 999 999,99. La prueba mostró que el porcentaje de ahorro se redondeaba a 0,00 % y provocaba una clasificación incorrecta como "sin ahorro". | Se corrigió la lógica para realizar la clasificación comparando directamente los montos en lugar de utilizar el porcentaje ya redondeado. Commit `1fe9435`. | Ivannia Porras |
| 15/08/2026 | Manejo de zona horaria en persistencia | Se comprobó el guardado de valores `DateTimeOffset` mediante Npgsql y se detectó que no acepta desplazamientos distintos de cero en ese contexto. | Se corrigió el manejo de fechas normalizando los valores a UTC en el límite de persistencia. Commit `99304a1`. | Ivannia Porras |
| 15/08/2026 | Configuración de concurrencia | Se revisó la configuración del control de concurrencia utilizada con Npgsql 9. | Se comprobó que `UseXminAsConcurrencyToken` ya no está disponible, por lo que la configuración se realizó manualmente. Commit `99304a1`. | Ivannia Porras |
| 15/08/2026 | Repositorios y pruebas | Se revisó el orden en que fueron desarrollados los repositorios y sus pruebas. | Se identificó que, a diferencia del resto del proyecto, los repositorios fueron escritos antes que sus pruebas. Esta situación se mantuvo documentada de forma explícita en `bitacora-xp.md`. Commit `ad58065`. | Ivannia Porras |
| 18/08/2026 | Consulta de ofertas | Se ejecutó la consulta de ofertas y se detectó un error 500 producido porque EF Core no podía traducir los filtros aplicados después de proyectar el resultado a otro tipo. | Se reescribió la consulta para aplicar correctamente los filtros y evitar el error de traducción de EF Core. Commit `30b13d7`. | Ivannia Porras |

### Segunda mitad — Anyelina Chacón Mora

> **Esta tabla la completa Anyelina.** Certifica su propia comprensión del código que
> construyó, así que no puede escribirla otra persona. Debe indicar, por cada elemento
> asistido: qué probó, qué corrigió y qué rechazó del resultado generado.
>
> Como punto de partida, el historial registra al menos una corrección suya sobre una
> propuesta de la herramienta: el commit *«corrige la vigencia que se mostraba un día antes
> al pasarla a hora local»*, del 20/08/2026.

| Fecha | Elemento revisado | Qué se probó | Qué se corrigió o rechazó | Revisado por |
|---|---|---|---|---|
| 20/08/2026 | Fecha de vigencia del tipo de cambio | *(por completar)* | *(por completar)* | Anyelina Chacón |
| | *(por completar)* | | | Anyelina Chacón |
| | *(por completar)* | | | Anyelina Chacón |

Estas validaciones nos sirven también como preparación para la defensa del proyecto, ya que
debemos ser capaces de explicar las decisiones realizadas, identificar por qué se produjo
cada problema y modificar el código en caso de que la persona docente lo solicite.

## 6. Responsabilidad

Las integrantes del equipo son responsables de **comprender, probar, corregir y defender**
todo el código entregado. "La herramienta lo generó" no constituye una explicación válida
de una decisión de diseño ni de un error. La persona docente puede solicitar explicación
oral o modificación en vivo de cualquier parte del proyecto.

Una herramienta de inteligencia artificial **no es una integrante adicional del equipo**
y no sustituye la programación en parejas.
