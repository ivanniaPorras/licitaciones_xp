# Sistema de Gestión de Licitaciones

Proyecto final del curso **ITI-822 · Metodologías Ágiles de Desarrollo de Software**
Universidad Técnica Nacional · Ingeniería en Tecnologías de Información · IIC-2026

Aplicación web modular para administrar licitaciones, proveedores, ofertas económicas,
niveles de aprobación y tipos de cambio. La moneda oficial y fuente de verdad es el
**colón costarricense (CRC)**; la visualización en USD es una representación calculada
que nunca modifica los valores persistidos.

El proceso de desarrollo se rige **exclusivamente por Extreme Programming (XP)**.

## Integrantes

| Nombre | Correo |
|---|---|
| Ivannia Porras Miranda | ivaporrasm@gmail.com |
| Anyelina Chacón Mora | nglnchacon@gmail.com |

## Ejecución en tres comandos

```bash
cp .env.example .env          # definir POSTGRES_USER y POSTGRES_PASSWORD
docker compose up --build     # levanta la aplicación y PostgreSQL 16
dotnet test                   # ejecuta las pruebas (requiere Docker para integración)
```

Detalle completo en [docker.md](docker.md) y [pruebas.md](pruebas.md).

## Entrega evaluable

Las 35 historias de usuario están terminadas. El estado de cada una y su trazabilidad a
pruebas y commits se consultan en [historias-usuario.md](historias-usuario.md).

| Verificación | Resultado |
|---|---|
| `dotnet build -c Release` | 0 errores, 0 advertencias |
| `dotnet format --verify-no-changes --severity warn` | sin diferencias |
| Pruebas unitarias | 244 superadas |
| Pruebas de integración | 103 superadas |
| Pruebas de navegador | 7 superadas |
| Cobertura de dominio y aplicación | 98,6 % y 88,2 % |
| Cobertura del proyecto completo | 81,2 % |

## Índice de la documentación

Esta carpeta es la **única forma de documentación del proyecto**. No se entregan
documentos Word, PDF, PowerPoint ni anexos externos.

### Proceso y planificación XP

| Documento | Contenido |
|---|---|
| [vision-alcance.md](vision-alcance.md) | Propósito del sistema, alcance incluido y excluido, actores, supuestos y glosario. |
| [historias-usuario.md](historias-usuario.md) | Historias desde la perspectiva del cliente, con prioridad, estimación, criterios de aceptación y trazabilidad. |
| [plan-xp.md](plan-xp.md) | Planning Game, plan de liberación, plan de las cuatro iteraciones y reglas de trabajo del equipo. |
| [bitacora-xp.md](bitacora-xp.md) | Registro por iteración: velocidad observada, evidencia TDD, refactorizaciones y retroalimentación del cliente. |
| [uso-ia.md](uso-ia.md) | Declaración del uso de herramientas de inteligencia artificial y validaciones realizadas. |

### Diseño y construcción

| Documento | Contenido |
|---|---|
| [arquitectura-general.md](arquitectura-general.md) | Capas, dirección de dependencias, decisiones de arquitectura y diagramas. |
| [modelo-datos.md](modelo-datos.md) | Diagrama entidad-relación, entidades, índices, restricciones, auditoría y concurrencia. |
| [integracion-modulos.md](integracion-modulos.md) | Cómo cooperan los módulos, flujos de extremo a extremo y límites entre componentes. |
| [api.md](api.md) | Endpoints, contratos, ejemplos, errores y colección reproducible de solicitudes. |

### Verificación y despliegue

| Documento | Contenido |
|---|---|
| [pruebas.md](pruebas.md) | Estrategia de pruebas, aplicación de TDD, comandos de ejecución y cobertura. |
| [docker.md](docker.md) | Construcción de la imagen, Docker Compose y demostración de persistencia. |
| [kubernetes.md](kubernetes.md) | Manifiestos, despliegue, sondas, almacenamiento persistente y evidencias. |

Los manifiestos de Kubernetes viven en [../k8s/](../k8s/) y los archivos de contenedor
—`Dockerfile` y `compose.yaml`— en la raíz del repositorio.

### Documentación por módulo

Cada archivo desarrolla los ocho encabezados fijos: propósito, responsabilidades,
dependencias, entradas, salidas, reglas, errores y pruebas.

| Módulo | Documento |
|---|---|
| Licitaciones | [modulos/licitaciones.md](modulos/licitaciones.md) |
| Proveedores | [modulos/proveedores.md](modulos/proveedores.md) |
| Ofertas | [modulos/ofertas.md](modulos/ofertas.md) |
| Niveles de aprobación | [modulos/niveles-aprobacion.md](modulos/niveles-aprobacion.md) |
| Tipo de cambio | [modulos/tipo-cambio.md](modulos/tipo-cambio.md) |
| Interfaz web | [modulos/interfaz-web.md](modulos/interfaz-web.md) |
| API REST | [modulos/api-rest.md](modulos/api-rest.md) |
| Persistencia | [modulos/persistencia.md](modulos/persistencia.md) |

Las imágenes y evidencias se almacenan en [assets/](assets/) y se enlazan desde
los archivos anteriores. Ahí vive también
[assets/licitaciones.http](assets/licitaciones.http), la colección reproducible de
solicitudes que recorre el flujo completo de la API.
