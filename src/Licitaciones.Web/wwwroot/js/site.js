// Alterna entre colones y dólares los montos que las vistas marcan con data-crc.
//
// La conversión que manda es la del servidor, en ConversionMonedaService, y es la que
// expone la API. Aquí se repite la misma división para poder cambiar de moneda sin
// recargar la página: los colones del atributo data-crc nunca se tocan, así que volver a
// colones siempre recupera el valor exacto que está almacenado.
(function () {
    'use strict';

    var CLAVE_PREFERENCIA = 'licitaciones.moneda';
    var DECIMALES = 2;

    var alternador = document.getElementById('alternador-moneda');
    if (!alternador) {
        // Sin tasa vigente no se dibuja el alternador y todo queda en colones.
        return;
    }

    var tasa = parseFloat(alternador.getAttribute('data-tasa'));
    var vigencia = alternador.getAttribute('data-vigencia');
    var leyenda = document.getElementById('alternador-moneda-tasa');
    var botones = alternador.querySelectorAll('[data-moneda]');

    var formatoUSD = new Intl.NumberFormat('es-CR', {
        style: 'currency',
        currency: 'USD',
        minimumFractionDigits: DECIMALES,
        maximumFractionDigits: DECIMALES
    });

    function convertirADolares(montoCRC) {
        // Se redondea alejándose de cero, igual que en el servidor.
        var factor = Math.pow(10, DECIMALES);
        return Math.round((montoCRC / tasa) * factor) / factor;
    }

    function mostrar(moneda) {
        var montos = document.querySelectorAll('[data-crc]');

        for (var i = 0; i < montos.length; i++) {
            var elemento = montos[i];

            // La primera vez se guarda el texto en colones tal como lo formateó el
            // servidor con la cultura es-CR, para poder restituirlo sin recalcularlo.
            if (elemento.getAttribute('data-crc-texto') === null) {
                elemento.setAttribute('data-crc-texto', elemento.textContent.trim());
            }

            if (moneda === 'USD') {
                elemento.textContent = formatoUSD.format(
                    convertirADolares(parseFloat(elemento.getAttribute('data-crc'))));
            } else {
                elemento.textContent = elemento.getAttribute('data-crc-texto');
            }
        }

        for (var j = 0; j < botones.length; j++) {
            var activo = botones[j].getAttribute('data-moneda') === moneda;
            botones[j].classList.toggle('active', activo);
            botones[j].setAttribute('aria-pressed', activo ? 'true' : 'false');
        }

        // La tasa y su fecha acompañan siempre al monto convertido.
        leyenda.textContent = moneda === 'USD'
            ? 'Tasa ' + tasa.toLocaleString('es-CR', {
                minimumFractionDigits: 4,
                maximumFractionDigits: 4
            }) + ' colones por dólar, vigente desde el ' + vigencia
            : '';
    }

    for (var k = 0; k < botones.length; k++) {
        botones[k].addEventListener('click', function (evento) {
            var moneda = evento.currentTarget.getAttribute('data-moneda');
            window.localStorage.setItem(CLAVE_PREFERENCIA, moneda);
            mostrar(moneda);
        });
    }

    mostrar(window.localStorage.getItem(CLAVE_PREFERENCIA) === 'USD' ? 'USD' : 'CRC');
})();
