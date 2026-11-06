import { rocket } from './datastar-rocket.1.0.4.js';

rocket('fve-copy-reveal', {
  mode: 'light', scope: false,
  setup: ({ host, $$, cleanup, effect }) => {
    const events = new AbortController();
    let disposed = false, queued = false;
    $$('revealed', host.getAttribute('revealed') === 'true');
    $$('message', '');
    const field = () => host.querySelector('input');
    const observe = () => observer.observe(host, { childList: true, subtree: true, attributes: true, attributeFilter: ['revealed', 'type', 'disabled'] });
    const synchronize = () => {
      observer.disconnect();
      const input = field();
      if (!input) { observe(); return; }
      const type = $$.revealed ? 'text' : 'password';
      if (input.type !== type) input.type = type;
      const reveal = host.querySelector('[data-fve-copy-reveal-action=reveal]');
      const label = $$.revealed ? 'Hide' : 'Reveal';
      if (reveal) {
        if (reveal.getAttribute('title') !== label) reveal.setAttribute('title', label);
        if (reveal.hasAttribute('aria-label') && reveal.getAttribute('aria-label') !== label) reveal.setAttribute('aria-label', label);
        const text = reveal.querySelector('[data-fve-copy-reveal-label]');
        if (text && text.textContent !== label) text.textContent = label;
      }
      for (const icon of host.querySelectorAll('[data-fve-copy-reveal-icon]')) icon.style.display = (icon.dataset.fveCopyRevealIcon === 'hide') === $$.revealed ? '' : 'none';
      const output = host.querySelector('output');
      if (output) { if (output.textContent !== $$.message) output.textContent = $$.message; output.style.display = $$.message ? '' : 'none'; }
      observe();
    };
    const schedule = () => {
      if (queued || disposed) return;
      queued = true;
      queueMicrotask(() => { queued = false; if (!disposed && host.isConnected) synchronize(); });
    };
    const observer = new MutationObserver(records => {
      if (records.some(record => record.target === host && record.attributeName === 'revealed')) $$.revealed = host.getAttribute('revealed') === 'true';
      schedule();
    });
    host.addEventListener('click', event => {
      const action = event.target.closest('[data-fve-copy-reveal-action]');
      const input = field();
      if (!action || action.closest('fve-copy-reveal') !== host || input?.disabled || action.disabled || event.defaultPrevented) return;
      if (action.dataset.fveCopyRevealAction === 'reveal') { $$.revealed = !$$.revealed; return; }
      const labelled = (input.getAttribute('aria-labelledby') || '').split(/\s+/).map(id => input.ownerDocument.getElementById(id)?.textContent?.trim()).filter(Boolean).join(' ');
      const label = labelled || input.getAttribute('aria-label') || [...(input.labels || [])].map(label => label.textContent.trim()).join(' ') || 'Value';
      $$.message = '';
      const report = message => { if (!disposed && host.isConnected) $$.message = message; };
      if (!navigator.clipboard?.writeText) report(`Could not copy ${label}. Check clipboard permissions.`);
      else navigator.clipboard.writeText(input.value).then(() => report(`${label} copied.`)).catch(() => report(`Could not copy ${label}. Check clipboard permissions.`));
    }, { signal: events.signal });
    effect(() => { $$.revealed; $$.message; schedule(); });
    synchronize();
    cleanup(() => { disposed = true; events.abort(); observer.disconnect(); });
  },
});
