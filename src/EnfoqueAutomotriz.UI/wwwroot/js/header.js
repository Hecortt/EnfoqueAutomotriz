// obra360 - Header (dropdown de usuario + centro de notificaciones)
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        // ── Dropdown de usuario ──────────────────────────────────────────
        var userMenuBtn = document.getElementById('user-menu-btn');
        var userMenu = document.getElementById('user-menu');

        userMenuBtn?.addEventListener('click', function (e) {
            e.stopPropagation();
            notificationMenu?.classList.add('hidden');
            userMenu?.classList.toggle('hidden');
        });

        document.addEventListener('click', function (e) {
            if (!userMenu?.contains(e.target) && !userMenuBtn?.contains(e.target)) {
                userMenu?.classList.add('hidden');
            }
        });

        // ── Centro de notificaciones ─────────────────────────────────────
        var notificationBtn = document.getElementById('notification-btn');
        var notificationMenu = document.getElementById('notification-menu');
        var notificationDot = document.getElementById('notification-dot');
        var notificationList = document.getElementById('notification-list');
        var markAllBtn = document.getElementById('notification-mark-all-read');
        var loaded = false;

        function timeAgo(dateStr) {
            var diffMs = Date.now() - new Date(dateStr).getTime();
            var mins = Math.floor(diffMs / 60000);
            if (mins < 1) return 'Ahora mismo';
            if (mins < 60) return 'Hace ' + mins + ' min';
            var hrs = Math.floor(mins / 60);
            if (hrs < 24) return 'Hace ' + hrs + ' h';
            var days = Math.floor(hrs / 24);
            return 'Hace ' + days + ' d';
        }

        function iconFor(tipo) {
            // TipoNotificacion (enum numérico serializado por la API): 1=Info, 2=Success, 3=Warning, 4=Error
            switch (tipo) {
                case 4: case 'error': return { icon: 'circle-alert', cls: 'text-danger' };
                case 3: case 'warning': return { icon: 'triangle-alert', cls: 'text-warning' };
                case 2: case 'success': return { icon: 'circle-check', cls: 'text-success' };
                default: return { icon: 'info', cls: 'text-info' };
            }
        }

        function renderNotifications(items) {
            if (!notificationList) return;
            if (!items || items.length === 0) {
                notificationList.innerHTML = '<div class="header-notification-empty">No tienes notificaciones</div>';
                return;
            }
            notificationList.innerHTML = items.map(function (n) {
                var ic = iconFor(n.tipo);
                var href = n.url ? ('href="' + n.url + '"') : '';
                var tag = n.url ? 'a' : 'div';
                return '<' + tag + ' ' + href + ' class="header-notification-item ' + (n.leida ? '' : 'is-unread') + '" data-id="' + n.id + '">' +
                    '<i data-lucide="' + ic.icon + '" class="w-4 h-4 shrink-0 mt-0.5 ' + ic.cls + '"></i>' +
                    '<div class="min-w-0 flex-1">' +
                    '<p class="header-notification-title">' + n.titulo + '</p>' +
                    '<p class="header-notification-body">' + n.mensaje + '</p>' +
                    '<p class="header-notification-time">' + timeAgo(n.createdAt) + '</p>' +
                    '</div></' + tag + '>';
            }).join('');
            if (typeof lucide !== 'undefined') lucide.createIcons();

            notificationList.querySelectorAll('.header-notification-item.is-unread').forEach(function (item) {
                item.addEventListener('click', function () {
                    var id = this.getAttribute('data-id');
                    this.classList.remove('is-unread');
                    marcarLeida(id);
                });
            });
        }

        async function marcarLeida(id) {
            try {
                await apiRequest('/api/v1/notificaciones/' + id + '/leer', 'POST');
                refreshUnreadCount();
            } catch (e) { /* silencioso */ }
        }

        async function refreshUnreadCount() {
            if (!notificationDot) return;
            try {
                var res = await apiRequest('/api/v1/notificaciones/no-leidas-count');
                var data = await res.json();
                var count = data?.result || 0;
                notificationDot.classList.toggle('hidden', !count);
            } catch (e) { /* silencioso */ }
        }

        async function loadNotifications() {
            if (!notificationList) return;
            try {
                var res = await apiRequest('/api/v1/notificaciones/recientes');
                var data = await res.json();
                renderNotifications(data?.result || []);
            } catch (e) {
                notificationList.innerHTML = '<div class="header-notification-empty">Error al cargar notificaciones</div>';
            }
        }

        notificationBtn?.addEventListener('click', function (e) {
            e.stopPropagation();
            userMenu?.classList.add('hidden');
            var wasHidden = notificationMenu?.classList.contains('hidden');
            notificationMenu?.classList.toggle('hidden');
            if (wasHidden && !loaded) {
                loaded = true;
                loadNotifications();
            }
        });

        document.addEventListener('click', function (e) {
            if (!notificationMenu?.contains(e.target) && !notificationBtn?.contains(e.target)) {
                notificationMenu?.classList.add('hidden');
            }
        });

        markAllBtn?.addEventListener('click', async function (e) {
            e.stopPropagation();
            try {
                await apiRequest('/api/v1/notificaciones/leer-todas', 'POST');
                notificationList?.querySelectorAll('.header-notification-item.is-unread').forEach(function (item) {
                    item.classList.remove('is-unread');
                });
                refreshUnreadCount();
            } catch (e2) { /* silencioso */ }
        });

        if (notificationDot) {
            refreshUnreadCount();
            setInterval(refreshUnreadCount, 60000);
        }
    });
})();
