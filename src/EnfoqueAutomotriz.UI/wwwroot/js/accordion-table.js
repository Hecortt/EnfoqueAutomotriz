/**
 * Accordion Table — filas expandibles
 * Uso: agregar data-accordion-toggle al <tr> principal y data-accordion-detail al <tr> de detalle.
 * El botón con data-accordion-btn dentro del tr principal controla el toggle.
 */
(function () {
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-accordion-btn]');
        if (!btn) return;

        var row = btn.closest('tr');
        if (!row) return;

        var detail = row.nextElementSibling;
        if (!detail || !detail.hasAttribute('data-accordion-detail')) return;

        var isOpen = !detail.classList.contains('hidden');
        detail.classList.toggle('hidden', isOpen);

        var icon = btn.querySelector('[data-lucide]');
        if (icon) {
            icon.style.transform = isOpen ? '' : 'rotate(180deg)';
        }

        btn.setAttribute('aria-expanded', String(!isOpen));
    });

    document.addEventListener('click', function (e) {
        var expandBtn = e.target.closest('[data-accordion-expand-all]');
        var collapseBtn = e.target.closest('[data-accordion-collapse-all]');

        if (expandBtn) {
            var container = expandBtn.closest('.card') || document;
            container.querySelectorAll('[data-accordion-detail]').forEach(function (row) {
                row.classList.remove('hidden');
            });
            container.querySelectorAll('[data-accordion-btn]').forEach(function (btn) {
                btn.setAttribute('aria-expanded', 'true');
                var icon = btn.querySelector('[data-lucide]');
                if (icon) icon.style.transform = 'rotate(180deg)';
            });
        }

        if (collapseBtn) {
            var container = collapseBtn.closest('.card') || document;
            container.querySelectorAll('[data-accordion-detail]').forEach(function (row) {
                row.classList.add('hidden');
            });
            container.querySelectorAll('[data-accordion-btn]').forEach(function (btn) {
                btn.setAttribute('aria-expanded', 'false');
                var icon = btn.querySelector('[data-lucide]');
                if (icon) icon.style.transform = '';
            });
        }
    });
})();
