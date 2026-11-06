import { rocket } from './datastar-rocket.1.0.4.js';

rocket('fve-code-block', {
  mode: 'light', scope: false,
  setup: ({ host, cleanup }) => {
    const events = new AbortController();
    let disposed = false, rendering, rendered, generation = 0, copyGeneration = 0, timer;
    const render = () => {
      const code = host.querySelector('[data-docs-copy-source]');
      if (!code || !window.fsharpDocsCode) return;
      const source = code.textContent, markup = code.innerHTML, language = code.className;
      if (rendered?.code === code && rendered.markup === markup && rendered.language === language || rendering?.code === code && rendering.source === source) return;
      const ticket = ++generation;
      rendering = { code, source };
      window.fsharpDocsCode.ensure().then(() => {
        if (disposed || ticket !== generation || !code.isConnected || !host.contains(code) || code.textContent !== source) return;
        window.Prism?.highlightElement?.(code);
        rendered = { code, markup: code.innerHTML, language };
      }).catch(error => { if (!disposed && host.isConnected) throw error; }).finally(() => { if (ticket === generation) rendering = undefined; });
    };
    host.addEventListener('click', async event => {
      const button = event.target.closest('[data-docs-copy-button]');
      if (!button || button.closest('fve-code-block') !== host || event.defaultPrevented) return;
      const ticket = ++copyGeneration, label = button.querySelector('[data-docs-copy-label]');
      window.clearTimeout(timer); delete button.dataset.copied; delete button.dataset.copyError;
      try {
        await navigator.clipboard.writeText(host.querySelector('[data-docs-copy-source]')?.textContent || '');
        if (disposed || ticket !== copyGeneration || !host.contains(button)) return;
        if (label) label.textContent = 'Copied'; button.title = 'Copied'; button.dataset.copied = 'true';
      } catch {
        if (disposed || ticket !== copyGeneration || !host.contains(button)) return;
        if (label) label.textContent = 'Copy failed'; button.title = 'Copy failed'; button.dataset.copyError = 'true';
      }
      timer = window.setTimeout(() => {
        if (label) label.textContent = ''; button.title = button.getAttribute('aria-label');
        delete button.dataset.copied; delete button.dataset.copyError;
      }, 1600);
    }, { signal: events.signal });
    const observer = new MutationObserver(render);
    observer.observe(host, { childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: ['class'] });
    host.addEventListener('datastar-scope-children', render, { signal: events.signal });
    host.addEventListener('fve-code-render', render, { signal: events.signal });
    render();
    cleanup(() => { disposed = true; generation++; copyGeneration++; events.abort(); observer.disconnect(); window.clearTimeout(timer); });
  },
});
