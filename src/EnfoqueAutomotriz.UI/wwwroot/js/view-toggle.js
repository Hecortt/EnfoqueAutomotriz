/**
 * View Toggle — lista ↔ tarjetas con persistencia en localStorage
 * Uso: data-view-toggle="modulo" en el contenedor, data-view="list" y data-view="cards" en los contenidos
 */
(function () {
    function getPreference(module) {
        try { return localStorage.getItem('viewMode_' + module) || 'list'; }
        catch (e) { return 'list'; }
    }

    function setPreference(module, mode) {
        try { localStorage.setItem('viewMode_' + module, mode); }
        catch (e) { /* ignore */ }
    }

    function applyView(container, mode) {
        var listEl = container.querySelector('[data-view="list"]');
        var cardsEl = container.querySelector('[data-view="cards"]');
        var btnList = container.querySelector('[data-view-btn="list"]');
        var btnCards = container.querySelector('[data-view-btn="cards"]');

        if (listEl) listEl.classList.toggle('hidden', mode !== 'list');
        if (cardsEl) cardsEl.classList.toggle('hidden', mode !== 'cards');

        if (btnList) {
            btnList.classList.toggle('bg-primary', mode === 'list');
            btnList.classList.toggle('text-white', mode === 'list');
            btnList.classList.toggle('text-text-3', mode !== 'list');
        }
        if (btnCards) {
            btnCards.classList.toggle('bg-primary', mode === 'cards');
            btnCards.classList.toggle('text-white', mode === 'cards');
            btnCards.classList.toggle('text-text-3', mode !== 'cards');
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-view-toggle]').forEach(function (container) {
            var module = container.getAttribute('data-view-toggle');
            var mode = getPreference(module);
            applyView(container, mode);
        });
    });

    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-view-btn]');
        if (!btn) return;
        var container = btn.closest('[data-view-toggle]');
        if (!container) return;
        var module = container.getAttribute('data-view-toggle');
        var mode = btn.getAttribute('data-view-btn');
        setPreference(module, mode);
        applyView(container, mode);
    });
})();
