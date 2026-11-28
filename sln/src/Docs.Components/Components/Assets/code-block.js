import { rocket } from './datastar-rocket.1.0.5.js';

rocket('fve-code-block', {
  mode: 'light',
  setup: ({ host, cleanup }) => {
    const events = new AbortController();
    let disposed = false, rendering, rendered, generation = 0, copyGeneration = 0, timer;
    const literalSource = () => {
      const code = host.querySelector('[data-docs-copy-source]');
      // Prism can normalize characters such as nonbreaking spaces in its markup.
      return rendered && rendered.code === code && rendered.markup === code.innerHTML ? rendered.source : code?.textContent;
    };
    const render = () => {
      const code = host.querySelector('[data-docs-copy-source]');
      for (const button of host.querySelectorAll('[data-fve-code-download]')) button.hidden = false;
      const language = code?.className.match(/(?:^|\s)language-([\w-]+)/)?.[1];
      if (!language || language === 'text') return;
      const source = literalSource(), markup = code.innerHTML;
      if (rendered?.code === code && rendered.markup === markup && rendered.language === language || rendering?.code === code && rendering.markup === markup && rendering.language === language) return;
      const ticket = ++generation;
      rendering = { code, markup, language };
      import('./prism-highlight.js').then(({ highlight }) => {
        if (disposed || ticket !== generation || !host.contains(code) || code.innerHTML !== markup || !code.classList.contains(`language-${language}`)) return;
        const result = highlight(source, language);
        if (result != null) code.innerHTML = result;
        rendered = { code, markup: code.innerHTML, language, source };
      }).catch(error => { if (!disposed && host.isConnected) throw error; }).finally(() => { if (ticket === generation) rendering = undefined; });
    };
    host.addEventListener('click', async event => {
      const download = event.target.closest('[data-fve-code-download]');
      if (download?.closest('fve-code-block') === host && !event.defaultPrevented) {
        const source = literalSource();
        if (source == null) return;
        const href = URL.createObjectURL(new Blob([source], { type: 'text/plain;charset=utf-8' }));
        const link = document.createElement('a');
        link.href = href;
        link.download = download.dataset.fveCodeDownload;
        link.click();
        window.setTimeout(() => URL.revokeObjectURL(href), 1000);
        return;
      }
      const button = event.target.closest('[data-docs-copy-button]');
      if (!button || button.closest('fve-code-block') !== host || event.defaultPrevented) return;
      const ticket = ++copyGeneration, label = button.querySelector('[data-docs-copy-label]');
      window.clearTimeout(timer); delete button.dataset.copied; delete button.dataset.copyError;
      try {
        await navigator.clipboard.writeText(literalSource() ?? '');
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
