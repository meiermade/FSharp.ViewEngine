import { rocket } from './datastar-rocket.1.0.4.js';

rocket('fve-toggle-button', {
  mode: 'light', scope: false,
  setup: ({ host, cleanup }) => {
    const events = new AbortController();
    host.addEventListener('click', event => {
      const button = event.target.closest('button');
      if (!button || button.closest('fve-toggle-button') !== host || button.disabled || host.hasAttribute('readonly') || event.defaultPrevented || host.hasAttribute('controlled')) return;
      button.setAttribute('aria-pressed', String(button.getAttribute('aria-pressed') !== 'true'));
      button.dispatchEvent(new Event('change', { bubbles: true }));
    }, { signal: events.signal });
    cleanup(() => events.abort());
  },
});
