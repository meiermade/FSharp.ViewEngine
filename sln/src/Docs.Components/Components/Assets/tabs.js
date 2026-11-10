import { rocket } from './datastar-rocket.1.0.4.js';

rocket('fve-tabs', {
  mode: 'light', scope: false,
  setup: ({ host, $$, effect, cleanup }) => {
    const events = new AbortController();
    let queued = false, disposed = false;
    const tabs = () => [...host.querySelectorAll('[data-fve-tab-value]')].filter(tab => tab.closest('fve-tabs') === host);
    const available = () => tabs().filter(tab => !tab.disabled && tab.getAttribute('aria-disabled') !== 'true');
    const serverSelected = () => host.getAttribute('selection') === 'server';
    $$('selected', host.getAttribute('value'));
    const observe = () => observer.observe(host, { childList: true, subtree: true, attributes: true, attributeFilter: ['value', 'disabled', 'aria-disabled', 'data-fve-tab-value'] });
    const synchronize = () => {
      observer.disconnect();
      const enabled = available();
      if (serverSelected()) $$.selected = host.getAttribute('value');
      if (!enabled.some(tab => tab.dataset.fveTabValue === $$.selected)) $$.selected = enabled[0]?.dataset.fveTabValue;
      for (const tab of tabs()) {
        const selected = tab.dataset.fveTabValue === $$.selected;
        tab.setAttribute('aria-selected', String(selected));
        tab.tabIndex = selected ? 0 : -1;
        const panel = host.ownerDocument.getElementById(tab.getAttribute('aria-controls'));
        if (!serverSelected() && panel && host.contains(panel)) panel.hidden = !selected;
      }
      observe();
    };
    const schedule = () => {
      if (queued || disposed) return;
      queued = true;
      queueMicrotask(() => { queued = false; if (!disposed && host.isConnected) synchronize(); });
    };
    const observer = new MutationObserver(records => {
      if (records.some(record => record.target === host && record.attributeName === 'value')) $$.selected = host.getAttribute('value');
      schedule();
    });
    const choose = (tab, focus = false) => {
      if (!available().includes(tab)) return;
      if (serverSelected()) {
        if (focus) tab.focus();
        tab.click();
        return;
      }
      $$.selected = tab.dataset.fveTabValue;
      synchronize();
      if (focus) tab.focus();
      host.dispatchEvent(new Event('change', { bubbles: true }));
    };
    host.addEventListener('click', event => {
      const tab = event.target.closest('[data-fve-tab-value]');
      if (tab?.closest('fve-tabs') === host && !event.defaultPrevented && !serverSelected()) choose(tab);
    }, { signal: events.signal });
    host.addEventListener('keydown', event => {
      const tab = event.target.closest('[data-fve-tab-value]');
      if (tab?.closest('fve-tabs') !== host || event.defaultPrevented) return;
      const enabled = available(), index = enabled.indexOf(tab);
      const keys = host.getAttribute('orientation') === 'vertical' ? ['ArrowUp', 'ArrowDown'] : ['ArrowLeft', 'ArrowRight'];
      const target = event.key === 'Home' ? enabled[0] : event.key === 'End' ? enabled.at(-1)
        : event.key === keys[0] ? enabled.at((index - 1 + enabled.length) % enabled.length)
        : event.key === keys[1] ? enabled.at((index + 1) % enabled.length) : null;
      if (target) { event.preventDefault(); choose(target, true); }
    }, { signal: events.signal });
    host.addEventListener('datastar-scope-children', synchronize, { signal: events.signal });
    effect(() => { $$.selected; schedule(); });
    synchronize();
    cleanup(() => { disposed = true; events.abort(); observer.disconnect(); });
  },
});
