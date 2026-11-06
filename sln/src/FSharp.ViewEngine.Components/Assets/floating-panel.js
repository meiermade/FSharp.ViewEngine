import { rocket } from './datastar-rocket.1.0.4.js';

rocket('fve-floating-panel', {
  mode: 'light', scope: false,
  setup: ({ host, $$, effect, cleanup }) => {
    const events = new AbortController();
    $$('state', host.getAttribute('state') || 'open');
    const parts = () => [...host.querySelectorAll('[data-fve-floating-panel-state]')].filter(part => part.closest('fve-floating-panel') === host);
    const synchronize = () => {
      for (const part of parts()) part.style.display = part.dataset.fveFloatingPanelState === $$.state ? '' : 'none';
    };
    const observer = new MutationObserver(() => { $$.state = host.getAttribute('state') || 'open'; });
    observer.observe(host, { attributes: true, attributeFilter: ['state'] });
    host.addEventListener('click', event => {
      const action = event.target.closest('[data-fve-floating-panel-action]');
      if (!action || action.closest('fve-floating-panel') !== host || action.disabled || event.defaultPrevented) return;
      $$.state = action.dataset.fveFloatingPanelAction;
      synchronize();
      const panel = parts().find(part => part.dataset.fveFloatingPanelState === 'open');
      host.dispatchEvent(new CustomEvent('fve-floating-panel-state', { bubbles: true, detail: { id: panel.id, state: $$.state } }));
      const focus = $$.state === 'open' ? host.querySelector('h2') : parts().find(part => part.dataset.fveFloatingPanelState === $$.state);
      queueMicrotask(() => { if (host.isConnected) focus?.focus(); });
    }, { signal: events.signal });
    host.addEventListener('datastar-scope-children', synchronize, { signal: events.signal });
    effect(synchronize);
    cleanup(() => { events.abort(); observer.disconnect(); });
  },
});
