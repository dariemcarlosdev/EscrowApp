// NexTruzt landing motion — GSAP + ScrollTrigger, self-hosted (CSP: script-src 'self').
// Purposeful reveal only (frontend-aesthetics rule): subtle fade + small Y, meaning-driven, not decorative.
// Content is visible by default in CSS/HTML — if GSAP is unavailable or reduced-motion is on, nothing hides.
(function () {
  'use strict';

  function prefersReducedMotion() {
    try { return window.matchMedia('(prefers-reduced-motion: reduce)').matches; }
    catch (e) { return false; }
  }

  function ready() {
    return !!(window.gsap && window.ScrollTrigger);
  }

  var registered = false;

  window.nexMotion = {
    // Idempotent; safe to call after every render / SPA navigation.
    init: function () {
      // Respect the user and fail-open: leave content visible, do nothing.
      if (prefersReducedMotion() || !ready()) return;

      if (!registered) {
        window.gsap.registerPlugin(window.ScrollTrigger);
        registered = true;
      }

      var gsap = window.gsap;

      // Single elements: fade + small rise as they enter the viewport.
      gsap.utils.toArray('[data-reveal]').forEach(function (el) {
        if (el.dataset.revealed === '1') return;
        el.dataset.revealed = '1';
        gsap.from(el, {
          opacity: 0,
          y: 18,
          duration: 0.45,
          ease: 'power2.out',
          scrollTrigger: {
            trigger: el,
            start: 'top 88%',
            toggleActions: 'play none none reverse'
          }
        });
      });

      // Groups: stagger the direct children (cards, list items, steps).
      gsap.utils.toArray('[data-reveal-stagger]').forEach(function (group) {
        if (group.dataset.revealed === '1') return;
        group.dataset.revealed = '1';
        gsap.from(group.children, {
          opacity: 0,
          y: 16,
          duration: 0.4,
          ease: 'power2.out',
          stagger: 0.07,
          scrollTrigger: {
            trigger: group,
            start: 'top 85%',
            toggleActions: 'play none none reverse'
          }
        });
      });

      window.ScrollTrigger.refresh();
    }
  };

  // Re-scan after Blazor enhanced navigation (event dispatched by theme.js).
  window.addEventListener('spa:navigation', function () {
    window.requestAnimationFrame(function () {
      if (window.nexMotion) window.nexMotion.init();
    });
  });
})();
