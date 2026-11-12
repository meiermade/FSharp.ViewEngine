import { rocket } from './datastar-rocket.1.0.4.js';

rocket('fve-resizable', {
  mode: 'light', scope: false,
  setup: ({ host, $$, cleanup, effect }) => {
    const events = new AbortController();
    const number = (name, fallback) => Number(host.getAttribute(name) ?? fallback);
    const storageKey = () => host.getAttribute('persistence-key');
    const initial = () => {
      const fallback = number('initial-size', 50);
      try {
        const key = storageKey(), saved = key ? localStorage.getItem(`fve-resizable:${key}`) : null;
        if (saved !== null) {
          const value = Number(saved);
          if (Number.isFinite(value) && (host.hasAttribute('collapsed-size') && value === number('collapsed-size', 0) || value >= number('min-size', 20) && value <= number('max-size', 80))) return value;
        }
      } catch { /* Storage is optional; resizing remains available. */ }
      return fallback;
    };
    const remember = () => {
      try { const key = storageKey(); if (key) localStorage.setItem(`fve-resizable:${key}`, String($$.size)); } catch { /* Optional preference. */ }
    };
    $$('size', initial());
    let drag;
    const handle = () => host.querySelector(':scope > [data-fve-resize-handle]');
    const horizontal = () => host.getAttribute('orientation') !== 'vertical';
    const set = value => { $$.size = Math.min(number('max-size', 80), Math.max(number('min-size', 20), value)); remember(); };
    const toggle = () => { $$.size = $$.size <= number('min-size', 20) ? number('initial-size', 50) : number('collapsed-size', 0); remember(); };
    const synchronize = () => {
      // Subscribe even when a morph connects this host before its panels arrive.
      const size = $$.size;
      const leading = host.querySelector(':scope > [data-fve-resize-leading]');
      if (leading) leading.style.flexBasis = `${size}%`;
      handle()?.setAttribute('aria-valuenow', String(Math.round(size)));
    };
    host.addEventListener('pointerdown', event => {
      const target = handle();
      if (!target?.contains(event.target) || event.button !== 0 || !event.isPrimary) return;
      target.setPointerCapture(event.pointerId);
      drag = { point: horizontal() ? event.clientX : event.clientY, size: $$.size };
    }, { signal: events.signal });
    host.addEventListener('pointermove', event => {
      if (!drag || !handle()?.hasPointerCapture(event.pointerId)) return;
      const box = host.getBoundingClientRect(), extent = horizontal() ? box.width : box.height;
      if (extent) set(drag.size + ((horizontal() ? event.clientX : event.clientY) - drag.point) / extent * 100);
    }, { signal: events.signal });
    host.addEventListener('lostpointercapture', () => { drag = undefined; }, { signal: events.signal });
    host.addEventListener('keydown', event => {
      if (!handle()?.contains(event.target) || event.defaultPrevented) return;
      const previous = horizontal() ? 'ArrowLeft' : 'ArrowUp', next = horizontal() ? 'ArrowRight' : 'ArrowDown';
      if (![previous, next, 'Home', 'End', ...(host.hasAttribute('collapsed-size') ? ['Enter'] : [])].includes(event.key)) return;
      event.preventDefault();
      if (event.key === 'Enter') toggle();
      else set(event.key === 'Home' ? number('min-size', 20) : event.key === 'End' ? number('max-size', 80) : $$.size + (event.key === previous ? -2 : 2));
    }, { signal: events.signal });
    host.addEventListener('dblclick', event => {
      if (host.hasAttribute('collapsed-size') && handle()?.contains(event.target) && !event.defaultPrevented) toggle();
    }, { signal: events.signal });
    const observer = new MutationObserver(() => { $$.size = initial(); });
    observer.observe(host, { attributes: true, attributeFilter: ['initial-size', 'min-size', 'max-size', 'collapsed-size', 'orientation', 'persistence-key'] });
    host.addEventListener('datastar-scope-children', synchronize, { signal: events.signal });
    effect(synchronize);
    cleanup(() => { events.abort(); observer.disconnect(); });
  },
});
