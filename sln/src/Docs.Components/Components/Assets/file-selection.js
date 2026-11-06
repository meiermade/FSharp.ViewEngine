import { rocket } from './datastar-rocket.1.0.4.js';
import { enhanceNativeControl } from './native-control.js';

rocket('fve-file-selection', {
  mode: 'light', scope: false,
  setup: context => {
    const { host, cleanup } = context;
    enhanceNativeControl(context, ['Enter', ' ']);
    const events = new AbortController();
    let queued = false, disposed = false;
    const field = () => host.querySelector('input[type=file]');
    const update = () => {
      const input = field(), clear = host.querySelector('[data-fve-file-clear]');
      if (!input || !clear) return;
      const labelled = (input.getAttribute('aria-labelledby') || '').split(/\s+/).map(id => input.ownerDocument.getElementById(id)?.textContent?.trim()).filter(Boolean).join(' ');
      const label = labelled || input.getAttribute('aria-label')?.trim() || [...(input.labels || [])].map(label => label.textContent.trim()).filter(Boolean).join(' ') || 'file';
      const name = 'Clear ' + label + ' selection' + (input.files.length === 1 ? ': ' + input.files[0].name : '');
      if (clear.getAttribute('aria-label') !== name) clear.setAttribute('aria-label', name);
      clear.hidden = input.files.length === 0;
    };
    const schedule = () => {
      if (queued || disposed) return;
      queued = true;
      queueMicrotask(() => { queued = false; if (!disposed && host.isConnected) update(); });
    };
    host.addEventListener('change', update, { signal: events.signal });
    host.addEventListener('click', event => {
      if (!event.target.closest('[data-fve-file-clear]') || event.defaultPrevented) return;
      const input = field();
      if (!input || input.disabled || input.readOnly) return;
      input.value = '';
      input.dispatchEvent(new Event('input', { bubbles: true }));
      input.dispatchEvent(new Event('change', { bubbles: true }));
      input.focus();
    }, { signal: events.signal });
    window.addEventListener('reset', event => {
      if (event.target === field()?.form) queueMicrotask(() => {
        if (!disposed && host.isConnected && !event.defaultPrevented) update();
      });
    }, { signal: events.signal });
    const observer = new MutationObserver(schedule);
    observer.observe(host, { childList: true, subtree: true, attributes: true, attributeFilter: ['disabled', 'readonly', 'aria-label', 'aria-labelledby'] });
    update();
    cleanup(() => { disposed = true; events.abort(); observer.disconnect(); });
  },
});
