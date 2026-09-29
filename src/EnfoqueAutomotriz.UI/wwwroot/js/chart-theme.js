/**
 * chart-theme.js — Tema visual compartido para todas las gráficas Chart.js del sistema.
 * Da un look "producto premium": gradientes, animaciones suaves, tooltips elegantes,
 * y contadores animados para las tarjetas de KPI.
 */
(function () {
    'use strict';

    if (typeof Chart === 'undefined') return;

    // ── Defaults globales de Chart.js ────────────────────────────────────────
    Chart.defaults.font.family = "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif";
    Chart.defaults.font.size = 12;
    Chart.defaults.color = '#64748B';
    Chart.defaults.animation.duration = 900;
    Chart.defaults.animation.easing = 'easeOutQuart';
    Chart.defaults.plugins.tooltip.enabled = true;
    Chart.defaults.plugins.tooltip.backgroundColor = 'rgba(15, 23, 42, 0.92)';
    Chart.defaults.plugins.tooltip.titleColor = '#F8FAFC';
    Chart.defaults.plugins.tooltip.bodyColor = '#E2E8F0';
    Chart.defaults.plugins.tooltip.padding = 10;
    Chart.defaults.plugins.tooltip.cornerRadius = 10;
    Chart.defaults.plugins.tooltip.titleFont = { weight: '700', size: 12 };
    Chart.defaults.plugins.tooltip.bodyFont = { size: 12 };
    Chart.defaults.plugins.tooltip.displayColors = true;
    Chart.defaults.plugins.tooltip.boxPadding = 6;
    Chart.defaults.plugins.tooltip.usePointStyle = true;
    Chart.defaults.elements.line.borderWidth = 3;
    Chart.defaults.elements.point.radius = 3;
    Chart.defaults.elements.point.hoverRadius = 6;
    Chart.defaults.elements.point.borderWidth = 2;
    Chart.defaults.elements.point.hitRadius = 8;
    Chart.defaults.elements.bar.borderRadius = 6;
    Chart.defaults.elements.bar.borderSkipped = false;

    // ── Paleta premium (gradientes de marca + acentos) ───────────────────────
    window.ChartTheme = {
        colors: {
            primary: '#4F46E5', primarySoft: 'rgba(79,70,229,0.15)',
            emerald: '#10B981', emeraldSoft: 'rgba(16,185,129,0.15)',
            amber: '#F59E0B', amberSoft: 'rgba(245,158,11,0.15)',
            red: '#EF4444', redSoft: 'rgba(239,68,68,0.15)',
            blue: '#3B82F6', blueSoft: 'rgba(59,130,246,0.15)',
            violet: '#8B5CF6', violetSoft: 'rgba(139,92,246,0.15)',
            slate: '#94A3B8', slateSoft: 'rgba(148,163,184,0.15)',
        },

        /** Crea un gradiente vertical suave para áreas de líneas/barras. */
        gradient: function (ctx, chartArea, colorFrom, colorTo) {
            if (!chartArea) return colorFrom;
            const g = ctx.createLinearGradient(0, chartArea.top, 0, chartArea.bottom);
            g.addColorStop(0, colorFrom);
            g.addColorStop(1, colorTo || 'rgba(255,255,255,0)');
            return g;
        },

        /** Devuelve un dataset de línea "premium": gradiente de relleno, curva suave, puntos con glow al hover. */
        lineDataset: function (label, data, color, ctx) {
            return {
                label: label,
                data: data,
                borderColor: color,
                backgroundColor: function (context) {
                    const chart = context.chart;
                    const { ctx: c, chartArea } = chart;
                    if (!chartArea) return 'transparent';
                    return window.ChartTheme.gradient(c, chartArea, color + '55', color + '00');
                },
                fill: true,
                tension: 0.4,
                pointBackgroundColor: '#fff',
                pointBorderColor: color,
                pointBorderWidth: 2,
                pointRadius: 3,
                pointHoverRadius: 7,
                pointHoverBackgroundColor: color,
                pointHoverBorderColor: '#fff',
                pointHoverBorderWidth: 2,
                cubicInterpolationMode: 'monotone'
            };
        },

        /** Opciones base reutilizables para gráficas de línea. */
        lineOptions: function (extra) {
            return Object.assign({
                responsive: true,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: { position: 'bottom', labels: { usePointStyle: true, pointStyle: 'circle', padding: 16, font: { weight: '600' } } }
                },
                scales: {
                    x: { grid: { display: false }, border: { display: false } },
                    y: { grid: { color: 'rgba(148,163,184,0.15)' }, border: { display: false } }
                }
            }, extra || {});
        },

        /** Opciones base para doughnut con hueco amplio + hover con "explosión" del segmento. */
        doughnutOptions: function (extra) {
            return Object.assign({
                responsive: true,
                cutout: '68%',
                animation: { animateRotate: true, animateScale: true, duration: 1000, easing: 'easeOutQuart' },
                plugins: {
                    legend: { position: 'bottom', labels: { usePointStyle: true, pointStyle: 'circle', padding: 14, font: { weight: '600', size: 11 } } }
                }
            }, extra || {});
        },

        /** Plugin: texto centrado en un doughnut (ej. total). Uso: plugins: [ChartTheme.centerTextPlugin('123', 'Total')] */
        centerTextPlugin: function (value, label) {
            return {
                id: 'centerText-' + Math.random().toString(36).slice(2),
                beforeDraw: function (chart) {
                    const { ctx, chartArea } = chart;
                    if (!chartArea) return;
                    const cx = (chartArea.left + chartArea.right) / 2;
                    const cy = (chartArea.top + chartArea.bottom) / 2;
                    ctx.save();
                    ctx.textAlign = 'center';
                    ctx.textBaseline = 'middle';
                    ctx.fillStyle = '#0F172A';
                    ctx.font = '700 22px Inter, sans-serif';
                    ctx.fillText(String(value), cx, cy - (label ? 10 : 0));
                    if (label) {
                        ctx.fillStyle = '#94A3B8';
                        ctx.font = '600 10px Inter, sans-serif';
                        ctx.fillText(label.toUpperCase(), cx, cy + 12);
                    }
                    ctx.restore();
                }
            };
        },

        /** Opciones base para radar "premium": relleno con gradiente, grid suave. */
        radarOptions: function (extra) {
            return Object.assign({
                responsive: true,
                animation: { duration: 1100, easing: 'easeOutQuart' },
                scales: {
                    r: {
                        min: 0, max: 100,
                        ticks: { stepSize: 20, showLabelBackdrop: false, color: '#CBD5E1', font: { size: 10 } },
                        grid: { color: 'rgba(148,163,184,0.25)' },
                        angleLines: { color: 'rgba(148,163,184,0.25)' },
                        pointLabels: { font: { size: 12, weight: '600' }, color: '#334155' }
                    }
                },
                plugins: { legend: { position: 'bottom', labels: { usePointStyle: true, pointStyle: 'circle', padding: 16, font: { weight: '600' } } } }
            }, extra || {});
        }
    };

    // ── Contador animado para tarjetas de KPI (data-count-to="123" data-decimals="1" data-prefix="$" data-suffix="%") ──
    window.animateCounters = function (root) {
        const scope = root || document;
        const elementos = scope.querySelectorAll('[data-count-to]');
        elementos.forEach(function (el) {
            const target = parseFloat(el.dataset.countTo);
            if (isNaN(target)) return;
            const decimals = parseInt(el.dataset.decimals || '0', 10);
            const prefix = el.dataset.prefix || '';
            const suffix = el.dataset.suffix || '';
            const duration = parseInt(el.dataset.duration || '1200', 10);
            const start = 0;
            const startTime = performance.now();

            function easeOutExpo(t) { return t === 1 ? 1 : 1 - Math.pow(2, -10 * t); }

            function frame(now) {
                const elapsed = now - startTime;
                const progress = Math.min(elapsed / duration, 1);
                const eased = easeOutExpo(progress);
                const valor = start + (target - start) * eased;
                el.textContent = prefix + valor.toLocaleString('es-MX', { minimumFractionDigits: decimals, maximumFractionDigits: decimals }) + suffix;
                if (progress < 1) requestAnimationFrame(frame);
                else el.textContent = prefix + target.toLocaleString('es-MX', { minimumFractionDigits: decimals, maximumFractionDigits: decimals }) + suffix;
            }
            requestAnimationFrame(frame);
        });
    };

    // ── Animación de entrada escalonada para tarjetas (.card, .kpi-card) ─────
    window.staggerCards = function (selector) {
        const cards = document.querySelectorAll(selector || '.stagger-in');
        cards.forEach(function (card, i) {
            card.style.opacity = '0';
            card.style.animation = 'fade-in-up 0.5s ease-out both';
            card.style.animationDelay = (i * 60) + 'ms';
        });
    };

    document.addEventListener('DOMContentLoaded', function () {
        window.animateCounters();
        window.staggerCards();
    });
})();
