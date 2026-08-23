# Imagen de los dos anfitriones del sistema. Cuál se construye lo decide el argumento
# PROYECTO, de modo que la web y la API comparten exactamente el mismo procedimiento y no
# hay dos archivos que mantener sincronizados.
#
# La construcción va en varias etapas: el compilador y el código fuente se quedan en la
# etapa de compilación y no llegan a la imagen final, que solo lleva el tiempo de
# ejecución y los archivos publicados.

ARG PROYECTO=Licitaciones.Web

# ---------------------------------------------------------------------------------------
# Etapa 1. Restauración de dependencias.
# Primero se copian solo los archivos de proyecto: mientras no cambien, Docker reutiliza
# la capa con los paquetes ya descargados aunque cambie el código.
# ---------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS restauracion
ARG PROYECTO
WORKDIR /origen

COPY Directory.Build.props ./
COPY src/Licitaciones.Domain/Licitaciones.Domain.csproj src/Licitaciones.Domain/
COPY src/Licitaciones.Application/Licitaciones.Application.csproj src/Licitaciones.Application/
COPY src/Licitaciones.Infrastructure/Licitaciones.Infrastructure.csproj src/Licitaciones.Infrastructure/
COPY src/Licitaciones.Api/Licitaciones.Api.csproj src/Licitaciones.Api/
COPY src/Licitaciones.Web/Licitaciones.Web.csproj src/Licitaciones.Web/

RUN dotnet restore "src/${PROYECTO}/${PROYECTO}.csproj"

# ---------------------------------------------------------------------------------------
# Etapa 2. Compilación y publicación.
# ---------------------------------------------------------------------------------------
FROM restauracion AS publicacion
ARG PROYECTO
WORKDIR /origen

COPY src/ src/

RUN dotnet publish "src/${PROYECTO}/${PROYECTO}.csproj" \
    -c Release \
    -o /publicado \
    --no-restore

# ---------------------------------------------------------------------------------------
# Etapa 3. Imagen final.
# Solo el tiempo de ejecución de ASP.NET Core y lo publicado.
# ---------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
ARG PROYECTO

# La cultura es-CR necesita los datos de globalización completos: sin esto, los colones no
# se formatearían como ₡1.250.000,00.
RUN apk add --no-cache icu-libs tzdata curl
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# La aplicación no corre como root. Si alguien lograra ejecutar código dentro del
# contenedor, no tendría privilegios de administración sobre él.
RUN addgroup -S licitaciones && adduser -S licitaciones -G licitaciones

WORKDIR /aplicacion
COPY --from=publicacion --chown=licitaciones:licitaciones /publicado ./

# El nombre del ensamblado se fija al construir, porque el punto de entrada no puede
# resolverse con una variable en tiempo de ejecución.
ENV ENSAMBLADO=${PROYECTO}.dll
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
USER licitaciones

ENTRYPOINT ["sh", "-c", "exec dotnet $ENSAMBLADO \"$@\"", "--"]
