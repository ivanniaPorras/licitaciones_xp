// Aplica el tema claro u oscuro antes de que se pinte la página.
//
// Este guion se carga en el <head> y de forma síncrona, no al final del cuerpo: si se
// ejecutara después, el navegador alcanzaría a pintar el tema equivocado y la página
// parpadearía al corregirse.
(function () {
    'use strict';

    var CLAVE = 'licitaciones.tema';

    function preferenciaDelSistema() {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches
            ? 'dark'
            : 'light';
    }

    function guardado() {
        try {
            return window.localStorage.getItem(CLAVE);
        } catch (error) {
            // Si el navegador tiene bloqueado el almacenamiento, se sigue con el del sistema.
            return null;
        }
    }

    window.TemaLicitaciones = {
        clave: CLAVE,
        preferenciaDelSistema: preferenciaDelSistema,
        guardado: guardado,
        aplicar: function (tema) {
            document.documentElement.setAttribute('data-bs-theme', tema);
        }
    };

    // La primera visita respeta lo que la persona tenga configurado en su sistema.
    window.TemaLicitaciones.aplicar(guardado() || preferenciaDelSistema());
})();
