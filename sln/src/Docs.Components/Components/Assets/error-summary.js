import { rocket } from './datastar-rocket.1.0.4.js';

rocket('fve-error-summary', {
  mode: 'light', scope: false,
  setup: ({ host, cleanup }) => {
    const events = new AbortController();
    if (host.hasAttribute('autofocus')) host.focus();
    host.addEventListener('click', event => {
      const link = event.target.closest('a[href^="#"]');
      if (!link || link.closest('fve-error-summary') !== host || event.defaultPrevented) return;
      host.ownerDocument.getElementById(decodeURIComponent(link.getAttribute('href').slice(1)))?.focus();
    }, { signal: events.signal });
    cleanup(() => events.abort());
  },
});
