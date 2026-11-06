import { rocket } from './datastar-rocket.1.0.4.js';

rocket('fve-resizable', {
  mode: 'light', scope: false,
  setup: ({ host, $$, cleanup, effect }) => {
    const events = new AbortController();
    const number = (name, fallback) => Number(host.getAttribute(name) ?? fallback);
    const initial = () => number('initial-size', 50);
    $$('size', initial());
    let drag;
    const handle = () => host.querySelector(':scope > [data-fve-resize-handle]');
    const horizontal = () => host.getAttribute('orientation') !== 'vertical';
    const set = value => { $$.size = Math.min(number('max-size', 80), Math.max(number('min-size', 20), value)); };
    const toggle = () => { $$.size = $$.size <= number('min-size', 20) ? initial() : number('collapsed-size', 0); };
    const synchronize = () => {
      const leading = host.querySelector(':scope > [data-fve-resize-leading]');
      if (leading) leading.style.flexBasis = `${$$.size}%`;
      handle()?.setAttribute('aria-valuenow', String(Math.round($$.size)));
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
    observer.observe(host, { attributes: true, attributeFilter: ['initial-size', 'min-size', 'max-size', 'collapsed-size', 'orientation'] });
    host.addEventListener('datastar-scope-children', synchronize, { signal: events.signal });
    effect(synchronize);
    cleanup(() => { events.abort(); observer.disconnect(); });
  },
});
