# Visión y alcance

Volver al [índice de la documentación](README.md).

## 1. Propósito del sistema

El Sistema de Gestión de Licitaciones permite a una organización publicar procesos de
compra, recibir ofertas económicas de proveedores registrados, identificar cuál de esas
ofertas conviene más y determinar quién tiene la autoridad para aprobarla.

Toda la información monetaria se administra en **colones costarricenses (CRC)**, que es
la moneda oficial y la única fuente de verdad. El sistema puede además mostrar los mismos
montos en dólares estadounidenses como ayuda de lectura, sin alterar jamás los valores
almacenados.

## 2. Problema que resuelve

En un proceso de compra llevado con hojas de cálculo y correo electrónico aparecen
problemas recurrentes:

- **No hay control del plazo.** Se reciben ofertas después de la fecha de cierre y luego
  se discute si valen o no.
- **Los proveedores se duplican.** "Empresa Central", "empresa central" y
  "EMPRESA  CENTRAL" terminan siendo tres registros distintos de la misma empresa.
- **Un proveedor presenta varias ofertas** para el mismo proceso y no queda claro cuál
  cuenta.
- **La comparación es manual y discutible.** Determinar la mejor oferta y cuánto se ahorra
  depende de quién arme el cuadro.
- **El nivel de autorización se decide caso por caso**, sin una regla escrita y uniforme.
- **La conversión a dólares se hace con tipos de cambio distintos** según quién arme el
  reporte, y no queda registro de cuál se usó.

El sistema convierte cada uno de esos puntos en una regla verificada por el programa, no
en un acuerdo verbal.

## 3. Alcance incluido

| Área | Qué incluye |
|---|---|
| **Licitaciones** | Registro, consulta, edición, borrado lógico y cambio de estado dentro del ciclo Borrador → Publicada → Cerrada, con código único y fecha de cierre seleccionada mediante calendario. |
| **Proveedores** | Registro, consulta, edición y borrado lógico, con nombre único tras normalización y restricción de caracteres permitidos. |
| **Ofertas** | Registro, consulta, edición y eliminación de ofertas sobre licitaciones publicadas y vigentes, con una única oferta por proveedor y licitación. |
| **Mejor oferta** | Selección de la oferta de menor monto, cálculo del porcentaje de ahorro y clasificación del resultado. |
| **Niveles de aprobación** | Tabla parametrizable de rangos de monto y su aprobador, sin traslapes y con un único rango abierto. |
| **Tipo de cambio** | Administración local de tasas CRC por USD, con un único registro activo y su fecha de vigencia. |
| **Interfaz web** | Página inicial explicativa, navegación, formularios validados, listados con paginación, filtrado y ordenamiento, modo claro y oscuro, y alternancia visual entre colones y dólares. |
| **API REST** | Endpoints versionados para los cinco módulos, documentados con OpenAPI y con errores expresados como `ProblemDetails`. |
| **Despliegue** | Ejecución con Docker Compose y despliegue en Kubernetes con almacenamiento persistente. |

## 4. Alcance explícitamente excluido

Estos puntos **no** forman parte del sistema. Excluirlos es una decisión, no un olvido.

| Excluido | Razón |
|---|---|
| **Autenticación y autorización de usuarios** | El enunciado no la solicita. Las acciones se describen por rol conceptual, pero el sistema no gestiona cuentas ni contraseñas. |
| **Reapertura de una licitación cerrada** | El enunciado la permite solo con una regla aprobada previamente por la persona docente. No se implementa; el estado Cerrada es terminal. |
| **Consulta de tipos de cambio a un servicio externo** | El sistema debe operar sin acceso a Internet. La tasa se administra localmente. |
| **Adjudicación formal, contratos y órdenes de compra** | Están fuera del proceso descrito, que termina al identificar la mejor oferta y su aprobador. |
| **Documentos adjuntos en las ofertas** | El enunciado define la oferta como un monto económico. |
| **Notificaciones por correo electrónico** | No se solicitan y añadirían una dependencia externa. |
| **Persistencia de montos en dólares** | Contradice la regla de que los colones son la única fuente de verdad. |
| **Múltiples monedas además de CRC y USD** | No se solicitan. |

## 5. Actores

| Actor | Descripción | Qué hace en el sistema |
|---|---|---|
| **Cliente** | Representa a la organización que necesita el sistema. En XP es quien prioriza las historias y acepta el resultado. | Define prioridades, escribe los criterios de aceptación y da retroalimentación al cierre de cada iteración. |
| **Encargado de compras** | Persona que opera el proceso día a día. | Registra proveedores, crea y publica licitaciones, registra ofertas y consulta la mejor oferta. |
| **Aprobador** | Persona con autoridad para autorizar un monto. Puede ser encargado de área, gerencia o junta directiva según el rango. | Consulta la mejor oferta y el nivel de aprobación que le corresponde. |
| **Administrador del tipo de cambio** | Responsable de mantener actualizada la tasa CRC por USD. | Registra nuevas tasas y activa la vigente. |
| **Programadora** | Cada integrante del equipo. | Escribe pruebas e implementación, refactoriza e integra. |

> El sistema no distingue técnicamente entre estos actores porque no hay autenticación.
> Se describen para dar sentido a las historias de usuario.

## 6. Supuestos

1. Los montos manejados caben en `numeric(18,2)`, suficiente para cifras del orden de
   billones de colones con dos decimales exactos.
2. La organización opera en la zona horaria `America/Costa_Rica`; las comparaciones
   internas se hacen en UTC y la presentación se convierte a la zona local.
3. Un proveedor presenta a lo sumo una oferta por licitación. Si desea corregirla, edita
   la existente mientras la licitación siga vigente.
4. La tasa de cambio la mantiene una persona de la organización. No se espera que cambie
   varias veces al día.
5. El volumen de datos es moderado: cientos de licitaciones y miles de ofertas, no
   millones. Esto justifica un monolito modular sobre PostgreSQL sin capas de caché.
6. Quien evalúe el proyecto tendrá Docker disponible para ejecutar las pruebas de
   integración y levantar el entorno.

## 7. Restricciones

1. **Metodología exclusivamente Extreme Programming.** Ningún otro marco ágil ni híbrido
   se emplea como marco rector, ni se usa su vocabulario. La justificación de la elección
   está en [plan-xp.md](plan-xp.md#1-por-qué-extreme-programming).
2. **PostgreSQL 16 o superior como único motor de base de datos**, incluso en las pruebas
   de integración, que se ejecutan contra un contenedor real.
3. **Los montos se representan con `decimal` y se almacenan como `numeric(18,2)`.**
4. **La documentación vive únicamente en `/docs`, en Markdown**, dentro de este mismo
   repositorio.
5. **No se versionan secretos** ni credenciales de ningún tipo.
6. **Los recursos de la interfaz se sirven localmente**: la aplicación debe funcionar sin
   acceso a Internet.
7. **La API nunca expone entidades de persistencia**, siempre objetos de transferencia.

## 8. Glosario

| Término | Definición |
|---|---|
| **Licitación** | Proceso de compra publicado por la organización, con un código único, un presupuesto estimado en colones y una fecha y hora de cierre. |
| **Estado de la licitación** | Etapa del ciclo de vida: *Borrador* (en preparación), *Publicada* (recibiendo ofertas) o *Cerrada* (terminada). |
| **Cierre funcional** | Condición de una licitación que ya no admite ofertas, sea porque su estado es Cerrada o porque la fecha y hora de cierre ya se alcanzaron, aunque el estado almacenado aún diga Publicada. |
| **Proveedor** | Empresa o persona que puede presentar ofertas. Se identifica por un nombre único una vez normalizado. |
| **Normalización del nombre** | Proceso de eliminar espacios laterales, reducir espacios repetidos, normalizar la representación Unicode y pasar a minúsculas, para detectar que dos escrituras distintas designan al mismo proveedor. |
| **Oferta** | Monto en colones que un proveedor propone para una licitación. Un proveedor presenta a lo sumo una oferta por licitación. |
| **Mejor oferta** | Oferta válida de menor monto. Si dos ofertas empatan en monto, gana la registrada primero. |
| **Ahorro** | Diferencia entre el presupuesto estimado y la mejor oferta, expresada como porcentaje del presupuesto. |
| **Clasificación** | Etiqueta que resume el resultado: *Sin ofertas válidas*, *Oferta conveniente*, *Oferta aceptable* u *Oferta válida sin ahorro*. |
| **Nivel de aprobación** | Rango de montos con un aprobador asignado. Los rangos no se traslapan y a lo sumo uno queda abierto por arriba. |
| **Aprobador** | Persona o instancia con autoridad para autorizar el monto de la mejor oferta, determinada por el nivel de aprobación aplicable. |
| **Tipo de cambio** | Cantidad de colones equivalente a un dólar, con su fecha de vigencia. Solo uno está activo a la vez. |
| **Borrado lógico** | Marcar un registro como eliminado mediante una fecha, conservándolo en la base de datos para no romper la trazabilidad de las ofertas asociadas. |
| **Historia de usuario** | Descripción breve de una necesidad, escrita desde la perspectiva de quien la tiene, con criterios de aceptación verificables. |
| **Iteración** | Período de trabajo de duración uniforme al final del cual se entrega software funcionando. |
| **Pequeña liberación** | Versión ejecutable y demostrable publicada al cierre de una iteración. |
| **Velocidad** | Cantidad de puntos de historia efectivamente terminados en una iteración, usada para planificar la siguiente. |
