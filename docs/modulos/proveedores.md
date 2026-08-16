# Módulo: Proveedores

> Estado: **capa de dominio terminada** (entrega 3). La persistencia, la interfaz y la API
> se completan en las entregas 4 y 5. Referencia de navegación: [../README.md](../README.md).

## 1. Propósito

Mantener el catálogo de empresas y personas que pueden presentar ofertas, garantizando que
cada una figure una sola vez.

## 2. Responsabilidades

- Reconocer que dos nombres escritos de forma distinta designan al mismo proveedor.
- Rechazar nombres con caracteres que no corresponden a una razón social.
- Conservar el nombre tal como lo escribió la persona usuaria, sin imponerle un formato.
- Impedir el borrado físico cuando existen ofertas relacionadas *(entrega 5)*.

## 3. Dependencias

Ninguna. Es el módulo más independiente del sistema: no conoce licitaciones ni ofertas.

## 4. Entradas

| Entrada | Origen | Formato |
|---|---|---|
| Nombre del proveedor | MVC / API *(entrega 5)* | `Proveedor.Crear(nombre)` |
| Nuevo nombre | MVC / API *(entrega 5)* | `Proveedor.Renombrar(nombre)` |

## 5. Salidas

| Salida | Destino | Formato |
|---|---|---|
| Proveedor creado | Repositorio | Entidad con `Nombre` y `NombreNormalizado` |
| Forma comparable de un nombre | Índice único de PostgreSQL | `NormalizadorNombreProveedor.Normalizar(nombre)` |

## 6. Reglas

| ID | Regla | Dónde vive |
|----|-------|-----------|
| R09 | El nombre es único tras recortar, colapsar espacios, normalizar Unicode y pasar a minúsculas | `NormalizadorNombreProveedor` |
| R10 | Solo se admiten letras, números, espacios, punto, coma y paréntesis | `ValidadorNombreProveedor` |
| R22 | No se elimina físicamente un proveedor con ofertas *(entrega 5)* | pendiente |

### Por qué la normalización Unicode va primero

`Normalizar` aplica la forma **KC** de Unicode antes que cualquier otro paso. El orden
importa por dos motivos:

1. La misma letra puede escribirse como carácter precompuesto o como letra base más marca
   combinante. Sin unificar esas formas, `Compañía` y `Compañía` serían proveedores
   distintos aunque se lean igual.
2. La forma de compatibilidad convierte separadores como el espacio duro en el espacio
   ordinario, que es el que el paso siguiente sabe colapsar. Si se colapsaran los espacios
   primero, un espacio duro sobreviviría y produciría un duplicado.

La expresión regular de caracteres admitidos usa `\p{L}` y `\p{N}` en lugar de rangos
ASCII, para no rechazar nombres con tildes o eñe.

## 7. Errores

| Código | HTTP | Mensaje |
|--------|------|---------|
| `PROVEEDOR_DUPLICADO` | 409 | Ya existe un proveedor con ese nombre. |
| `NOMBRE_INVALIDO` | 422 | El nombre solo admite letras, números, espacios, punto, coma y paréntesis. |
| `PROVEEDOR_CON_OFERTAS` | 409 | No se puede eliminar: existen ofertas relacionadas. |

## 8. Pruebas

| Prueba | Tipo | Regla |
|--------|------|-------|
| `NormalizadorNombreProveedorTests` | Unitaria | R09 |
| `ValidadorNombreProveedorTests` | Unitaria | R10 |
| `EntidadesDominioTests` | Unitaria | R09, R10 |

Los casos de escritura equivalente y los símbolos rechazados están tomados literalmente de
los ejemplos del enunciado.
