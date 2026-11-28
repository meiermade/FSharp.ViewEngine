import { rocket } from './datastar-rocket.1.0.5.js';

rocket('fve-masked-value', {
  mode: 'light',
  setup: ({ host, cleanup }) => {
    const events = new AbortController();
    const input = () => host.querySelector('input');
    const display = () => host.querySelector('[data-fve-masked-text]');
    let revealed = false;
    let presented;
    let previousValue;

    const label = field => {
      if (!field) return host.dataset.fveMaskedLabel || 'value';
      const labelled = (field.getAttribute('aria-labelledby') || '').split(/\s+/)
        .map(id => field.ownerDocument.getElementById(id)?.textContent?.trim()).filter(Boolean).join(' ');
      return labelled || field.getAttribute('aria-label') ||
        [...field.labels].map(node => node.textContent.trim()).filter(Boolean).join(' ') || 'value';
    };
    const synchronize = () => {
      const field = input(), value = display();
      if (value) {
        const text = value.dataset.fveMaskedText;
        if (value !== presented || text !== previousValue) revealed = false;
        presented = value;
        previousValue = text;
        const visible = revealed ? text : '••••••••';
        if (value.textContent !== visible) value.textContent = visible;
      }
      const toggle = host.querySelector('[data-fve-masked-action=toggle]');
      if (toggle) {
        const shown = field ? field.type === 'text' : revealed;
        if (toggle.hidden) toggle.hidden = false;
        const disabled = field?.disabled ?? false;
        if (toggle.disabled !== disabled) toggle.disabled = disabled;
        const name = `${shown ? 'Hide' : 'Show'} ${label(field)}`;
        if (toggle.getAttribute('aria-label') !== name) toggle.setAttribute('aria-label', name);
        if (toggle.title !== name) toggle.title = name;
        toggle.querySelector('[data-fve-mask-slash]')?.toggleAttribute('hidden', !shown);
      }
      const copy = host.querySelector('[data-fve-masked-action=copy]');
      if (copy?.hidden) copy.hidden = false;
    };
    host.addEventListener('click', async event => {
      const action = event.target.closest('[data-fve-masked-action]');
      const field = input();
      if (action?.closest('fve-masked-value') !== host || action.disabled || field?.disabled || event.defaultPrevented) return;
      if (action.dataset.fveMaskedAction === 'toggle') {
        if (field) {
          const start = field.selectionStart, end = field.selectionEnd, direction = field.selectionDirection;
          field.type = field.type === 'password' ? 'text' : 'password';
          if (start !== null && end !== null) field.setSelectionRange(start, end, direction);
        } else revealed = !revealed;
        synchronize();
      } else {
        const value = display(), status = host.querySelector('output');
        if (!value || !status) return;
        status.textContent = '';
        let message;
        try {
          await navigator.clipboard.writeText(value.dataset.fveMaskedText);
          message = `${label()} copied.`;
        } catch {
          message = `Could not copy ${label()}.`;
        }
        if (!events.signal.aborted && status.isConnected) status.textContent = message;
      }
    }, { signal: events.signal });
    synchronize();
    const observer = new MutationObserver(synchronize);
    observer.observe(host, { subtree: true, childList: true, attributes: true,
      attributeFilter: ['type', 'disabled', 'hidden', 'aria-label', 'aria-labelledby', 'data-fve-masked-text', 'data-fve-masked-label'] });
    cleanup(() => { events.abort(); observer.disconnect(); });
  },
});
