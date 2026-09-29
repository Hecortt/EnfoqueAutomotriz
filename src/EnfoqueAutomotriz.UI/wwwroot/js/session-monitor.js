/**
 * obra360 Session Monitor
 * 
 * Decodes the JWT exp claim from the meta tag and shows a warning
 * modal when the session is about to expire. The user can choose
 * to extend the session (page reload to trigger SlidingExpiration)
 * or log out immediately.
 */
(function () {
    'use strict';

    var WARNING_MINUTES = 5;
    var CHECK_INTERVAL  = 30000; // 30 s
    var warned = false;
    var expired = false;
    var timerInterval = null;

    function getJwtExpiry() {
        var meta = document.querySelector('meta[name="jwt-token"]');
        if (!meta) return null;
        var token = meta.getAttribute('content');
        if (!token) return null;
        try {
            var parts = token.split('.');
            if (parts.length !== 3) return null;
            var payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
            while (payload.length % 4) payload += '=';
            var decoded = JSON.parse(atob(payload));
            return decoded.exp ? decoded.exp * 1000 : null;
        } catch (e) {
            return null;
        }
    }

    function createModal() {
        var html = ''
            + '<div id="_sessionModal" class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-[9999] p-4 hidden" style="animation:none">'
            + '  <div class="bg-white rounded-2xl shadow-2xl w-full max-w-sm overflow-hidden animate-modal-in">'
            + '    <div class="bg-gradient-to-r from-amber-500 to-orange-500 px-6 py-5 text-center">'
            + '      <div class="w-14 h-14 rounded-full bg-white/20 flex items-center justify-center mx-auto mb-3">'
            + '        <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>'
            + '      </div>'
            + '      <h3 class="text-white font-bold text-lg" id="_sessionTitle">Sesión por expirar</h3>'
            + '      <p class="text-white/80 text-sm mt-1" id="_sessionSubtitle">Tu sesión expirará pronto</p>'
            + '    </div>'
            + '    <div class="px-6 py-5">'
            + '      <div class="text-center mb-4">'
            + '        <div class="inline-flex items-center gap-2 bg-amber-50 border border-amber-200 rounded-xl px-4 py-2.5">'
            + '          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#D97706" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>'
            + '          <span class="font-mono font-bold text-xl text-amber-700" id="_sessionTimer">5:00</span>'
            + '        </div>'
            + '      </div>'
            + '      <p class="text-sm text-gray-500 text-center" id="_sessionMsg">Si no extiendes tu sesión, se cerrará automáticamente y los cambios no guardados se perderán.</p>'
            + '    </div>'
            + '    <div class="px-6 pb-5 flex gap-3">'
            + '      <a href="/Logout" id="_sessionLogout" class="flex-1 px-4 py-2.5 rounded-lg border border-gray-200 text-gray-600 text-sm font-medium text-center hover:bg-gray-50 transition-colors">Cerrar sesión</a>'
            + '      <button type="button" id="_sessionExtend" onclick="_sessionDoExtend()" class="flex-1 px-4 py-2.5 rounded-lg bg-primary text-white text-sm font-medium text-center hover:bg-primary-hover transition-colors shadow-sm">Extender sesión</button>'
            + '    </div>'
            + '  </div>'
            + '</div>';
        document.body.insertAdjacentHTML('beforeend', html);
    }

    function formatTime(ms) {
        if (ms <= 0) return '0:00';
        var totalSec = Math.ceil(ms / 1000);
        var min = Math.floor(totalSec / 60);
        var sec = totalSec % 60;
        return min + ':' + (sec < 10 ? '0' : '') + sec;
    }

    function showWarning(expiryMs) {
        var modal = document.getElementById('_sessionModal');
        if (!modal) return;
        modal.classList.remove('hidden');
        warned = true;

        updateTimer(expiryMs);
        if (timerInterval) clearInterval(timerInterval);
        timerInterval = setInterval(function () {
            updateTimer(expiryMs);
        }, 1000);
    }

    function updateTimer(expiryMs) {
        var remaining = expiryMs - Date.now();
        var timerEl = document.getElementById('_sessionTimer');
        var titleEl = document.getElementById('_sessionTitle');
        var subtitleEl = document.getElementById('_sessionSubtitle');
        var msgEl = document.getElementById('_sessionMsg');
        var extendBtn = document.getElementById('_sessionExtend');

        if (remaining <= 0) {
            if (timerEl) timerEl.textContent = '0:00';
            if (titleEl) titleEl.textContent = 'Sesión expirada';
            if (subtitleEl) subtitleEl.textContent = 'Tu sesión ha finalizado';
            if (msgEl) msgEl.textContent = 'Por seguridad, debes iniciar sesión nuevamente para continuar.';
            if (extendBtn) extendBtn.style.display = 'none';
            expired = true;
            if (timerInterval) { clearInterval(timerInterval); timerInterval = null; }

            var header = document.getElementById('_sessionModal')?.querySelector('.bg-gradient-to-r');
            if (header) {
                header.className = header.className
                    .replace('from-amber-500', 'from-red-500')
                    .replace('to-orange-500', 'to-red-600');
            }
            return;
        }

        if (timerEl) timerEl.textContent = formatTime(remaining);
    }

    window._sessionDoExtend = function () {
        if (expired) {
            window.location.href = '/Login';
            return;
        }
        window.location.reload();
    };

    function check() {
        if (warned) return;
        var expiryMs = getJwtExpiry();
        if (!expiryMs) return;

        var remaining = expiryMs - Date.now();
        var warningThreshold = WARNING_MINUTES * 60 * 1000;

        if (remaining <= 0) {
            showWarning(expiryMs);
        } else if (remaining <= warningThreshold) {
            showWarning(expiryMs);
        }
    }

    function init() {
        var expiryMs = getJwtExpiry();
        if (!expiryMs) return;

        createModal();
        check();
        setInterval(check, CHECK_INTERVAL);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
