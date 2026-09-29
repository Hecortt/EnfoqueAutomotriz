/**
 * Page Loader — barra de progreso delgada arriba (misma que usa HTMX, ver
 * #htmx-progress en _Layout.cshtml) + overlay con blur y marca "obra360"
 * animada, mostrados mientras el navegador carga una página nueva
 * (navegación) o se envía un formulario que hace un submit real (no AJAX).
 *
 * Diseño: la barra superior aparece instantánea (feedback inmediato, patrón
 * usado por GitHub/YouTube/Linear); el overlay centrado con la marca solo
 * aparece si la espera se alarga más de ~200ms, para no generar un "parpadeo"
 * en navegaciones casi instantáneas.
 *
 * No interfiere con:
 *  - Links con target="_blank", download, href="#", javascript:, mailto:, tel:
 *  - Clicks con Ctrl/Cmd/Shift (abrir en pestaña nueva) o botón central del mouse
 *  - Formularios manejados 100% por JS (fetch) que hacen event.preventDefault()
 *  - Elementos marcados con [data-no-loader]
 *
 * Se integra con `alfaConfirmSubmit` (ver _Layout.cshtml): como esa función
 * hace `form.submit()` de forma programática (lo cual NO dispara el evento
 * "submit" del formulario), el loader se activa manualmente ahí mismo justo
 * antes de enviar el formulario.
 */
(function () {
    var overlay;
    var boxShowTimeout = null;
    var hideTimeout = null;

    function getBar() {
        // Reutiliza la misma barra superior que ya usa HTMX (#htmx-progress,
        // ver _Layout.cshtml), para que la navegación completa y las
        // peticiones parciales de HTMX se sientan como un solo sistema visual.
        return document.getElementById('htmx-progress');
    }

    function ensureOverlay() {
        if (overlay) return overlay;
        overlay = document.createElement('div');
        overlay.id = 'pageLoaderOverlay';
        overlay.className = 'page-loader-overlay';
        overlay.innerHTML =
            '<div class="page-loader-box">' +
            '  <div class="page-loader-logo">' +
            '    <span class="page-loader-logo-ring"></span>' +
            '    <span class="page-loader-logo-ring page-loader-logo-ring-delay"></span>' +
            '    <span class="page-loader-logo-icon"><i data-lucide="wrench"></i></span>' +
            '  </div>' +
            '  <p class="page-loader-brand">EnfoqueAutomotriz<span class="page-loader-brand-sub">Taller</span></p>' +
            '  <p class="page-loader-subtext">Un momento, por favor</p>' +
            '</div>';
        document.body.appendChild(overlay);
        if (typeof lucide !== 'undefined') lucide.createIcons();
        return overlay;
    }

    window.showPageLoader = function (text) {
        var el = ensureOverlay();
        if (text) {
            var t = el.querySelector('.page-loader-subtext');
            if (t) t.textContent = text;
        }

        // Barra superior: feedback instantáneo, siempre.
        var bar = getBar();
        if (bar) {
            bar.classList.remove('done');
            bar.classList.add('active');
        }

        // Overlay con marca: solo si la espera se alarga (evita parpadeo en
        // navegaciones casi instantáneas).
        clearTimeout(boxShowTimeout);
        boxShowTimeout = setTimeout(function () {
            el.classList.add('active');
        }, 200);

        // Failsafe: si por alguna razón la navegación no ocurre (error de red,
        // el usuario cancela, etc.), no dejamos el overlay bloqueado para siempre.
        clearTimeout(hideTimeout);
        hideTimeout = setTimeout(window.hidePageLoader, 15000);
    };

    window.hidePageLoader = function () {
        clearTimeout(hideTimeout);
        clearTimeout(boxShowTimeout);
        if (overlay) overlay.classList.remove('active');

        var bar = getBar();
        if (bar) {
            bar.classList.remove('active');
            bar.classList.add('done');
            setTimeout(function () { bar.classList.remove('done'); }, 500);
        }
    };

    function isModifiedClick(e) {
        return e.defaultPrevented || e.button === 1 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey;
    }

    function isExternalOrSpecialLink(a) {
        var href = a.getAttribute('href') || '';
        if (!href || href.charAt(0) === '#') return true;
        if (/^(javascript:|mailto:|tel:)/i.test(href)) return true;
        if (a.target && a.target !== '' && a.target !== '_self') return true;
        if (a.hasAttribute('download')) return true;
        if (a.closest('[data-no-loader]')) return true;
        return false;
    }

    // Navegación por links normales
    document.addEventListener('click', function (e) {
        if (isModifiedClick(e)) return;
        var a = e.target.closest('a[href]');
        if (!a || isExternalOrSpecialLink(a)) return;
        window.showPageLoader();
    }, true);

    // Envío de formularios reales (no AJAX)
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form || form.closest('[data-no-loader]')) return;
        // Esperamos un tick: si algún handler llamó a preventDefault() (formularios
        // manejados por fetch/AJAX), no mostramos el loader.
        setTimeout(function () {
            if (!e.defaultPrevented) window.showPageLoader();
        }, 0);
    }, true);

    // Restaurar desde el cache de navegación (botón atrás/adelante)
    window.addEventListener('pageshow', function (e) {
        if (e.persisted) window.hidePageLoader();
    });

    document.addEventListener('DOMContentLoaded', window.hidePageLoader);
})();
