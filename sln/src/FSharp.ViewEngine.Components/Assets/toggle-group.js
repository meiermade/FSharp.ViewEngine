import { rocket } from './datastar-rocket.1.0.4.js';

rocket('fve-toggle-group', {
  mode: 'light', scope: false,
  setup: ({ host, $$, cleanup, effect }) => {
    const events = new AbortController();
    const options = { signal: events.signal };
    const multiple = host.hasAttribute('multiple');
    const buttons = () => [...host.querySelectorAll('[data-fve-toggle-value]')].filter(el => el.closest('fve-toggle-group') === host);
    const initialValues = () => multiple ? JSON.parse(host.getAttribute('values') || '[]') : [host.getAttribute('value')].filter(value => value !== null);
    let initial = initialValues(), disposed = false, pending = false;
    // Native values survive disconnection; host attributes remain the reset baseline.
    const values = host.querySelector('[data-fve-toggle-values]');
    $$('selected', values ? [...values.children].map(field => field.value) : [...initial]);
    const disabled = () => host.hasAttribute('disabled');
    const unavailable = () => disabled() || host.hasAttribute('readonly');
    const observe = () => observer.observe(host, { childList: true, subtree: true, attributes: true,
      attributeFilter: ['value', 'values', 'name', 'multiple', 'disabled', 'readonly', 'data-fve-toggle-value'] });
    const synchronize = () => {
      observer.disconnect();
      const selected = $$.selected;
      for (const button of buttons()) {
        button.setAttribute('aria-pressed', String(selected.includes(button.dataset.fveToggleValue)));
        button.disabled = unavailable() || button.hasAttribute('data-fve-toggle-disabled');
      }
      const values = host.querySelector('[data-fve-toggle-values]');
      if (values) {
        const existing = new Map([...values.children].map(field => [field.value, field]));
        selected.forEach((value, index) => {
          const field = existing.get(value) || document.createElement('input');
          field.type = 'hidden'; field.name = host.getAttribute('name'); field.value = value; field.disabled = disabled();
          if (values.children[index] !== field) values.insertBefore(field, values.children[index] || null);
          existing.delete(value);
        });
        for (const field of existing.values()) field.remove();
      }
      observe();
    };
    const schedule = () => {
      if (pending || disposed) return;
      pending = true;
      queueMicrotask(() => { pending = false; if (!disposed && host.isConnected) synchronize(); });
    };
    const authoritative = () => {
      initial = initialValues();
      $$.selected = [...initial];
      schedule();
    };
    const observer = new MutationObserver(records => {
      if (records.some(record => record.target === host && ['value', 'values'].includes(record.attributeName))) authoritative();
      else schedule();
    });
    host.addEventListener('datastar-scope-children', authoritative, options);
    host.addEventListener('click', event => {
      const button = event.target.closest('[data-fve-toggle-value]');
      if (!button || button.closest('fve-toggle-group') !== host || button.disabled || unavailable() || event.defaultPrevented) return;
      const value = button.dataset.fveToggleValue, selected = $$.selected;
      $$.selected = multiple ? selected.includes(value) ? selected.filter(item => item !== value) : [...selected, value]
        : host.hasAttribute('allow-empty') && selected.includes(value) ? [] : [value];
      synchronize();
      host.dispatchEvent(new Event('input', { bubbles: true }));
      host.dispatchEvent(new Event('change', { bubbles: true }));
    }, options);
    host.addEventListener('keydown', event => {
      const button = event.target.closest('[data-fve-toggle-value]');
      if (!button || button.closest('fve-toggle-group') !== host || unavailable()) return;
      const available = buttons().filter(item => !item.disabled), index = available.indexOf(button);
      const vertical = host.getAttribute('orientation') === 'vertical';
      const keys = vertical ? ['ArrowUp', 'ArrowDown'] : ['ArrowLeft', 'ArrowRight'];
      const target = event.key === 'Home' ? available[0] : event.key === 'End' ? available.at(-1)
        : event.key === keys[0] ? available.at((index - 1 + available.length) % available.length)
        : event.key === keys[1] ? available.at((index + 1) % available.length) : null;
      if (target) { event.preventDefault(); target.focus(); }
    }, options);
    window.addEventListener('reset', event => {
      const form = host.querySelector('input[type=hidden]')?.form || host.closest('form');
      if (event.target === form) queueMicrotask(() => {
        if (!disposed && host.isConnected && !event.defaultPrevented) { $$.selected = [...initial]; synchronize(); }
      });
    }, options);
    effect(() => { $$.selected; schedule(); });
    synchronize();
    cleanup(() => { disposed = true; events.abort(); observer.disconnect(); });
  },
});
