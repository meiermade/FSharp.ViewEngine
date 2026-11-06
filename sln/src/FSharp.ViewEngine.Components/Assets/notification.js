import { rocket } from './datastar-rocket.1.0.4.js';

rocket('fve-notification', {
  mode: 'light', scope: false,
  setup: ({ host, cleanup }) => {
    const events = new AbortController();
    let elapsed = 0, at = performance.now(), paused = true;
    const article = () => host.querySelector('[data-fve-notification]');
    const dismiss = reason => {
      const notification = article();
      if (!notification) return;
      const region = notification.closest('[data-fve-notification-region]');
      const id = notification.id;
      if (notification.contains(document.activeElement)) document.activeElement.blur();
      host.remove();
      region?.dispatchEvent(new CustomEvent('fve-notification-dismiss', { bubbles: true, detail: { id, reason } }));
    };
    const advance = () => {
      const notification = article();
      if (!notification) return;
      const now = performance.now();
      if (!paused) elapsed += Math.max(0, now - at);
      at = now;
      paused = document.hidden || notification.matches(':hover') || notification.matches(':focus-within');
      const ttl = Number(notification.dataset.fveNotificationTtl);
      if (!paused && ttl > 0 && elapsed >= ttl) dismiss('timeout');
    };
    host.addEventListener('click', event => {
      if (!event.defaultPrevented && event.target.closest('[data-fve-notification-dismiss]')) dismiss('dismiss');
    }, { signal: events.signal });
    for (const name of ['mouseenter', 'mouseleave', 'focusin', 'focusout']) host.addEventListener(name, () => queueMicrotask(() => { if (host.isConnected) advance(); }), { signal: events.signal });
    document.addEventListener('visibilitychange', advance, { signal: events.signal });
    advance();
    const timer = window.setInterval(advance, 100);
    cleanup(() => { events.abort(); window.clearInterval(timer); });
  },
});
