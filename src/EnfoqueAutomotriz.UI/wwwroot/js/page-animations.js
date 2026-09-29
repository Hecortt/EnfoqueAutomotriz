/**
 * obra360 Page Animations
 * 
 * Automatically applies staggered fade-in-up animations to common
 * page elements on load and after HTMX swaps.
 */
(function () {
    'use strict';

    var STAGGER_DELAY = 60;

    function animateElements() {
        var grids = document.querySelectorAll(
            '.grid:not([data-animated]), .space-y-4:not([data-animated]), .space-y-6:not([data-animated])'
        );

        grids.forEach(function (grid) {
            if (grid.closest('.modal-backdrop') || grid.closest('.flatpickr-calendar')) return;
            if (grid.closest('[data-animated]')) return;

            var isDirectPageChild = false;
            var parent = grid.parentElement;
            while (parent) {
                if (parent.classList.contains('animate-fade-in-up') || parent.tagName === 'MAIN') {
                    isDirectPageChild = true;
                    break;
                }
                if (parent.classList.contains('modal-body') || parent.classList.contains('card-body')) {
                    break;
                }
                parent = parent.parentElement;
            }

            if (!isDirectPageChild) return;

            var children = grid.children;
            if (children.length < 2 || children.length > 12) return;

            grid.setAttribute('data-animated', '1');

            for (var i = 0; i < children.length; i++) {
                var child = children[i];
                child.style.opacity = '0';
                child.style.animation = 'fade-in-up 0.4s ease-out ' + (i * STAGGER_DELAY) + 'ms both';
            }
        });
    }

    function init() {
        requestAnimationFrame(function () {
            animateElements();
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    document.addEventListener('htmx:afterSwap', function () {
        requestAnimationFrame(animateElements);
    });
})();
