# Kubernetes

Volver al [índice de la documentación](README.md).

Los manifiestos viven en [`/k8s`](../k8s) y despliegan el sistema completo en su propio
espacio de nombres, con almacenamiento persistente para la base de datos.

## 1. Clúster utilizado

Los manifiestos no dependen de ningún proveedor: usan únicamente objetos estándar y una
clase de almacenamiento por omisión. Se han pensado para un clúster local
—**minikube**, **kind** o el Kubernetes que trae Docker Desktop— y funcionan igual en uno
gestionado cambiando el tipo de los servicios.

| Requisito | Nota |
|---|---|
| Kubernetes | 1.27 o superior |
| Clase de almacenamiento por omisión | Necesaria para el volumen de PostgreSQL |
| Imágenes disponibles en el clúster | Ver la sección siguiente |

### Las imágenes deben estar en el clúster

Los manifiestos usan `imagePullPolicy: IfNotPresent` y no apuntan a ningún registro, así
que las imágenes tienen que existir en el nodo. Se construyen y se cargan así:

```bash
docker build -t licitaciones-web:1.0.0 --build-arg PROYECTO=Licitaciones.Web .
docker build -t licitaciones-api:1.0.0 --build-arg PROYECTO=Licitaciones.Api .

# minikube
minikube image load licitaciones-web:1.0.0
minikube image load licitaciones-api:1.0.0

# kind
kind load docker-image licitaciones-web:1.0.0
kind load docker-image licitaciones-api:1.0.0
```

Con Docker Desktop no hace falta cargarlas: comparte el mismo almacén de imágenes.

## 2. Orden de aplicación de los manifiestos

Los archivos van numerados y ese es el orden. El único paso manual es el secreto.

```bash
kubectl apply -f k8s/00-namespace.yaml
kubectl apply -f k8s/01-configmap.yaml

# El secreto real se crea a mano. Ver la sección 3.
kubectl create secret generic licitaciones-secret \
  --namespace licitaciones \
  --from-literal=POSTGRES_USER='usuario_real' \
  --from-literal=POSTGRES_PASSWORD='contrasena_real'

kubectl apply -f k8s/03-postgres.yaml
kubectl wait --namespace licitaciones --for=condition=ready pod -l app.kubernetes.io/name=postgres --timeout=180s

kubectl apply -f k8s/04-migraciones-job.yaml
kubectl wait --namespace licitaciones --for=condition=complete job/licitaciones-migraciones --timeout=180s

kubectl apply -f k8s/05-web.yaml
kubectl apply -f k8s/06-api.yaml
```

Al terminar:

| Servicio | Dirección |
|---|---|
| Aplicación web | http://localhost:30080 |
| API | http://localhost:30081 |
| Documentación interactiva | http://localhost:30081/swagger |

En minikube, `minikube service licitaciones-web --namespace licitaciones` abre la dirección
que corresponda.

Para desmontarlo todo basta con borrar el espacio de nombres, que se lleva consigo lo que
contiene:

```bash
kubectl delete namespace licitaciones
```

## 3. Creación del secreto real a partir del ejemplo

El repositorio versiona **solo** [`k8s/02-secret.example.yaml`](../k8s/02-secret.example.yaml),
con valores ficticios. Ese archivo no se aplica: existe para que quien despliegue sepa qué
claves espera el resto de los manifiestos sin tener que leerlos uno por uno.

El secreto real se crea con `kubectl create secret`, como se muestra arriba, y nunca entra
al repositorio. La configuración que no es sensible —nombre de la base, anfitrión, puerto,
entorno— vive aparte, en el mapa de configuración.

Ambos se consumen igual desde los contenedores, con `envFrom`, y la cadena de conexión se
arma juntando las dos fuentes:

```yaml
- name: ConnectionStrings__Default
  value: "Host=$(POSTGRES_HOST);Port=$(POSTGRES_PORT);Database=$(POSTGRES_DB);Username=$(POSTGRES_USER);Password=$(POSTGRES_PASSWORD)"
```

## 4. Ejecución controlada de las migraciones

Las migraciones **no se aplican al arrancar la aplicación**. Con dos réplicas, ambas
intentarían migrar a la vez sobre la misma base.

En su lugar hay un `Job` que corre una sola vez:

- Un contenedor de inicialización espera con `pg_isready` a que PostgreSQL acepte
  conexiones.
- El contenedor principal usa **la misma imagen de la API**, invocada con
  `--aplicar-migraciones`. Aplica lo pendiente, informa de qué hizo y termina.
- Si algo falla, sale con código distinto de cero y el Job se marca como fallido en lugar
  de dejar la aplicación arrancar sobre un esquema incompleto.

Su registro dice exactamente qué se aplicó:

```bash
kubectl logs --namespace licitaciones job/licitaciones-migraciones
```

El Job se conserva diez minutos tras terminar —`ttlSecondsAfterFinished`— para poder leer
ese registro, y luego se limpia solo.

## 5. Sondas y límites

Los dos despliegues declaran las **tres sondas** y sus límites de recursos.

| Sonda | Ruta | Qué responde | Qué pasa si falla |
|---|---|---|---|
| Arranque | `/health/live` | Que el proceso levantó | Da margen de dos minutos antes de que las otras actúen |
| Disponibilidad | `/health/ready` | Que además alcanza la base | El pod sale del servicio, pero no se reinicia |
| Vitalidad | `/health/live` | Que el proceso responde | El contenedor se reinicia |

La distinción importa. **La sonda de vitalidad no consulta la base a propósito**: si lo
hiciera, una base lenta o momentáneamente caída provocaría reinicios en cadena de una
aplicación que está perfectamente sana, y reiniciarla no arreglaría nada. Quien decide si
un pod recibe tráfico es la sonda de disponibilidad; quien decide si hay que reiniciarlo es
la de vitalidad.

| Recurso | Solicitado | Límite |
|---|---|---|
| Web y API, procesador | 100m | 500m |
| Web y API, memoria | 192Mi | 512Mi |
| PostgreSQL, procesador | 100m | 1 |
| PostgreSQL, memoria | 256Mi | 1Gi |

Los contenedores de la aplicación corren **sin privilegios**, con el sistema de archivos
raíz de solo lectura y sin ninguna capacidad del núcleo. Como .NET necesita escribir
temporales, se les monta un `emptyDir` en `/tmp`.

## 6. Conservación de datos tras reinicio

PostgreSQL se despliega como `StatefulSet` y no como `Deployment` porque una base de datos
tiene identidad: su volumen debe seguir siendo el mismo cuando el pod se recrea. La
plantilla de reclamación de volumen le da un disco propio que sobrevive al pod.

La comprobación es directa: se crea un dato, se borra el pod y se comprueba que sigue ahí.

```bash
# 1. Crear un proveedor
curl -s -X POST http://localhost:30081/api/v1/proveedores \
  -H "Content-Type: application/json" \
  -d '{"nombre":"Persistencia Kubernetes S.A."}'

# 2. Borrar el pod de la base. El StatefulSet lo recrea solo.
kubectl delete pod --namespace licitaciones postgres-0
kubectl wait --namespace licitaciones --for=condition=ready pod/postgres-0 --timeout=180s

# 3. El dato sigue ahí
curl -s "http://localhost:30081/api/v1/proveedores?busqueda=Persistencia"
```

El identificador devuelto en el paso 3 es el mismo del paso 1: no es un registro nuevo.

Mientras el pod se recrea, las réplicas de la web y de la API siguen vivas pero salen del
servicio, porque su sonda de disponibilidad falla. Vuelven solas en cuanto la base
responde, sin que nadie tenga que reiniciarlas.

## 7. Evidencias de despliegue

Comandos para dejar constancia del estado del despliegue:

```bash
kubectl get all --namespace licitaciones
kubectl get pvc --namespace licitaciones
kubectl describe pod --namespace licitaciones -l app.kubernetes.io/name=web
kubectl logs --namespace licitaciones job/licitaciones-migraciones
```

Las capturas correspondientes se archivan en [assets/](assets/).

> **Salvedad honesta.** Los manifiestos se revisaron y se validaron sintácticamente, y las
> imágenes que despliegan son exactamente las que verifica Docker Compose en
> [docker.md](docker.md). El despliegue completo sobre un clúster real queda pendiente de
> ejecutarse en la máquina donde se haga la demostración, y sus evidencias se adjuntarán en
> `assets/` en ese momento. Se anota aquí en lugar de dar por probado algo que todavía no
> se ejecutó.
