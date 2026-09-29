/**
 * Indicador de scroll horizontal en tablas anchas.
 * Agrega la clase `has-scroll` a `.table-container` cuando el contenido
 * se desborda horizontalmente, mostrando un degradado a la derecha
 * (definido en input.css) que indica que hay más columnas por ver.
 */
(function () {
    function checkOverflow(el) {
        var hasOverflow = el.scrollWidth - el.clientWidth > 4;
        var scrolledToEnd = el.scrollLeft + el.clientWidth >= el.scrollWidth - 4;
        el.classList.toggle('has-scroll', hasOverflow && !scrolledToEnd);
    }

    function checkAll() {
        document.querySelectorAll('.table-container').forEach(checkOverflow);
    }

    document.addEventListener('scroll', function (e) {
        if (e.target && e.target.classList && e.target.classList.contains('table-container')) {
            checkOverflow(e.target);
        }
    }, true);

    window.addEventListener('resize', checkAll);
    document.addEventListener('DOMContentLoaded', checkAll);

    // Reintenta tras cargas dinámicas (HTMX, fetch, etc.) y al abrir modales/tabs.
    if (window.ResizeObserver) {
        var ro = new ResizeObserver(function () { checkAll(); });
        document.addEventListener('DOMContentLoaded', function () {
            document.querySelectorAll('.table-container').forEach(function (el) { ro.observe(el); });
        });
    }

    document.addEventListener('htmx:afterSwap', checkAll);
    setTimeout(checkAll, 300);
})();
