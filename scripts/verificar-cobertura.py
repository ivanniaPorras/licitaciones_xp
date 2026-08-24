"""Comprueba que la cobertura de líneas alcance los umbrales acordados con el cliente.

La cobertura es una condición de entrega, no una métrica informativa: si baja del umbral,
este script termina con código distinto de cero y la integración continua bloquea el
cambio.

Une los informes de todas las ejecuciones de prueba antes de medir. Una misma línea puede
estar cubierta por una prueba unitaria en un informe y no aparecer cubierta en otro; contar
cada informe por separado daría un porcentaje más bajo que el real.

Uso:
    python scripts/verificar-cobertura.py [directorio_de_resultados]
"""

import glob
import os
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict

UMBRAL_POR_CAPA = 80.0
UMBRAL_TOTAL = 70.0
CAPAS_EXIGIDAS = ("Licitaciones.Domain", "Licitaciones.Application")


def main() -> int:
    resultados = sys.argv[1] if len(sys.argv) > 1 else "TestResults"
    patron = os.path.join(resultados, "**", "coverage.cobertura.xml")
    informes = glob.glob(patron, recursive=True)

    if not informes:
        print(f"No se encontró ningún informe de cobertura en {patron}")
        return 1

    cubiertas: dict[str, set] = defaultdict(set)
    totales: dict[str, set] = defaultdict(set)

    for informe in informes:
        for paquete in ET.parse(informe).getroot().iter("package"):
            nombre = paquete.get("name")
            for clase in paquete.iter("class"):
                fichero = clase.get("filename")
                for linea in clase.iter("line"):
                    clave = (fichero, linea.get("number"))
                    totales[nombre].add(clave)
                    if int(linea.get("hits")) > 0:
                        cubiertas[nombre].add(clave)

    fallos: list[str] = []
    suma_cubiertas = suma_totales = 0

    print(f"{'Ensamblado':34s} {'Cubiertas':>10s} {'Totales':>10s} {'%':>8s}")
    for nombre in sorted(totales):
        c, t = len(cubiertas[nombre]), len(totales[nombre])
        suma_cubiertas += c
        suma_totales += t
        porcentaje = 100 * c / t if t else 0.0
        print(f"{nombre:34s} {c:10d} {t:10d} {porcentaje:7.1f} %")

        if nombre in CAPAS_EXIGIDAS and porcentaje < UMBRAL_POR_CAPA:
            fallos.append(
                f"{nombre} está en {porcentaje:.1f} %, "
                f"por debajo del {UMBRAL_POR_CAPA:.0f} % exigido"
            )

    total = 100 * suma_cubiertas / suma_totales if suma_totales else 0.0
    print(f"{'TOTAL':34s} {suma_cubiertas:10d} {suma_totales:10d} {total:7.1f} %")

    if total < UMBRAL_TOTAL:
        fallos.append(
            f"El total está en {total:.1f} %, por debajo del {UMBRAL_TOTAL:.0f} % exigido"
        )

    for fallo in fallos:
        print(f"FALLO: {fallo}")

    return 1 if fallos else 0


if __name__ == "__main__":
    sys.exit(main())
