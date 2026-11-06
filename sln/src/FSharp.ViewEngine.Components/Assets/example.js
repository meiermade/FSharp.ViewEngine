import { rocket } from './datastar-rocket.1.0.4.js';
import './code-block.js';

rocket('fve-example', {
  mode: 'light', scope: false,
  setup: ({ host, $$, effect, cleanup }) => {
    const events = new AbortController(), options = { signal: events.signal };
    const owned = selector => [...host.querySelectorAll(selector)].filter(part => part.closest('fve-example') === host);
    const tabs = () => owned('[data-fve-example-tab]');
    const preview = () => owned('[data-docs-example-preview]')[0];
    const code = () => owned('[data-docs-example-code]')[0];
    const frame = () => owned('[data-docs-preview-frame]')[0];
    const handle = () => owned('[data-fve-example-resize]')[0];
    $$('tab', tabs().find(tab => tab.getAttribute('aria-selected') === 'true')?.dataset.fveExampleTab || host.getAttribute('initial-tab'));
    $$('width', frame()?.style.width || '100%');
    let drag;
    const extent = () => {
      if (!host.isConnected || !frame() || !handle()) return;
      handle().setAttribute('aria-valuenow', String(Math.round(frame().offsetWidth)));
      handle().setAttribute('aria-valuemax', String(frame().parentElement.clientWidth));
      handle().setAttribute('aria-valuemin', String(Math.min(240, frame().parentElement.clientWidth)));
    };
    const synchronize = () => {
      for (const tab of tabs()) {
        const active = tab.dataset.fveExampleTab === $$.tab;
        tab.setAttribute('aria-selected', String(active)); tab.tabIndex = active ? 0 : -1;
        if (active) tab.dataset.selected = 'true'; else delete tab.dataset.selected;
      }
      if (preview()) { preview().hidden = $$.tab !== 'preview'; preview().style.removeProperty('display'); }
      if (code()) { code().hidden = $$.tab !== 'code'; code().style.removeProperty('display'); }
      const widths = owned('[data-fve-example-widths]')[0]; if (widths) widths.hidden = $$.tab !== 'preview';
      for (const control of owned('[data-fve-example-width]')) control.setAttribute('aria-pressed', String(control.dataset.fveExampleWidth === $$.width));
      if (frame() && frame().style.width !== $$.width) frame().style.width = $$.width;
      extent();
    };
    const activate = () => queueMicrotask(() => {
      if (!host.isConnected) return;
      if ($$.tab === 'preview') host.dispatchEvent(new CustomEvent('fve-example-preview', { bubbles: true, detail: { panel: preview() } }));
      else code()?.dispatchEvent(new Event('fve-code-render'));
    });
    host.addEventListener('click', event => {
      if (event.defaultPrevented || event.target.closest('fve-example') !== host) return;
      const tab = event.target.closest('[data-fve-example-tab]'), width = event.target.closest('[data-fve-example-width]');
      if (tab) { $$.tab = tab.dataset.fveExampleTab; activate(); }
      else if (width) $$.width = width.dataset.fveExampleWidth;
    }, options);
    const resize = width => { const maximum = frame().parentElement.clientWidth; $$.width = Math.min(maximum, Math.max(Math.min(240, maximum), width)) + 'px'; };
    host.addEventListener('keydown', event => {
      if (event.defaultPrevented || event.target.closest('fve-example') !== host) return;
      const tab = event.target.closest('[data-fve-example-tab]');
      if (tab && ['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) {
        event.preventDefault(); const controls = tabs(), index = controls.indexOf(tab);
        const next = event.key === 'Home' ? controls[0] : event.key === 'End' ? controls.at(-1) : controls.at((index + (event.key === 'ArrowRight' ? 1 : -1) + controls.length) % controls.length);
        $$.tab = next.dataset.fveExampleTab; next.focus(); activate();
      } else if (event.target === handle() && ['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) {
        event.preventDefault();
        if (event.key === 'End') $$.width = '100%'; else resize(event.key === 'Home' ? 240 : frame().offsetWidth + (event.key === 'ArrowRight' ? 16 : -16));
      }
    }, options);
    host.addEventListener('pointerdown', event => {
      if (event.target.closest('[data-fve-example-resize]') !== handle() || event.target.closest('fve-example') !== host) return;
      drag = { id: event.pointerId, x: event.clientX, width: frame().getBoundingClientRect().width, handle: handle() };
      drag.handle.setPointerCapture(event.pointerId);
    }, options);
    host.addEventListener('pointermove', event => { if (drag?.id === event.pointerId && drag.handle.hasPointerCapture(event.pointerId)) resize(drag.width + event.clientX - drag.x); }, options);
    const release = () => { if (drag?.handle.hasPointerCapture(drag.id)) drag.handle.releasePointerCapture(drag.id); drag = undefined; };
    host.addEventListener('pointerup', release, options); host.addEventListener('pointercancel', release, options);
    const observer = new ResizeObserver(extent); if (preview()) observer.observe(preview());
    host.addEventListener('datastar-scope-children', synchronize, options);
    effect(synchronize); activate();
    cleanup(() => { events.abort(); observer.disconnect(); release(); });
  },
});
