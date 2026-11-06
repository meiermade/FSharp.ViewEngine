import { rocket } from './datastar-rocket.1.0.4.js';

// Mermaid's renderer/configuration are shared; instance DOM and stale-result ownership are not.
let queue = Promise.resolve(), renderId = 0;
const diagram = (source, theme) => {
  const render = async () => {
    const service = window.fsharpDocsMermaid;
    if (!service) throw new Error('Mermaid assets are unavailable.');
    await service.ensure();
    if (!service.hasApi()) throw new Error('Mermaid is unavailable.');
    window.mermaid.initialize({ startOnLoad: false, theme, securityLevel: service.security || 'antiscript', suppressErrorRendering: true });
    return window.mermaid.render('fsharp-docs-mermaid-' + ++renderId, source);
  };
  const result = queue.then(render, render); queue = result.catch(() => {}); return result;
};

rocket('fve-mermaid', {
  mode: 'light', scope: false,
  setup: ({ host, cleanup }) => {
    const events = new AbortController();
    let disposed = false, generation = 0, pending, applied;
    const status = (state, role, message) => {
      host.dataset.mermaidState = state;
      if (state === 'pending') { delete host.dataset.mermaidRenderedSource; host.setAttribute('aria-busy', 'true'); } else host.removeAttribute('aria-busy');
      const node = document.createElement('p'); node.className = 'm-0 text-center text-sm leading-relaxed text-[var(--fve-muted-text)]';
      node.dataset.mermaidStatus = 'true'; node.setAttribute('role', role); node.textContent = message; host.replaceChildren(node);
    };
    const theme = () => {
      const scheme = getComputedStyle(host).colorScheme;
      return scheme === 'dark' || scheme !== 'light' && document.documentElement.classList.contains('dark') ? 'dark' : 'neutral';
    };
    const render = (force = false) => {
      if (disposed || !host.isConnected || !force && !host.checkVisibility()) return;
      const source = host.dataset.mermaidSource || '', mode = theme(), key = JSON.stringify([source, mode]);
      if (!force && (pending === key || applied?.key === key && applied.markup === host.innerHTML)) return;
      const ticket = ++generation; pending = key;
      status('pending', 'status', 'Rendering diagram…');
      diagram(source, mode).then(({ svg, bindFunctions }) => {
        if (disposed || ticket !== generation || !host.isConnected) return;
        host.innerHTML = svg; bindFunctions?.(host); host.dataset.mermaidState = 'rendered'; host.dataset.mermaidRenderedSource = source; host.removeAttribute('aria-busy');
        applied = { key, markup: host.innerHTML };
      }).catch(() => {
        if (disposed || ticket !== generation || !host.isConnected) return;
        status('failed', 'alert', 'Diagram unavailable.'); applied = { key, markup: host.innerHTML };
      }).finally(() => { if (ticket === generation) pending = undefined; });
    };
    const observer = new MutationObserver(() => render());
    observer.observe(host, { childList: true, attributes: true, attributeFilter: ['data-mermaid-source'] });
    const appearance = new MutationObserver(() => render());
    for (let parent = host.parentElement; parent; parent = parent.parentElement) appearance.observe(parent, { attributes: true, attributeFilter: ['class', 'style'] });
    const visibility = new IntersectionObserver(entries => { if (entries.some(entry => entry.isIntersecting)) render(); }); visibility.observe(host);
    for (const name of ['fve-color-mode', 'fsharpdocs:colormode']) window.addEventListener(name, () => render(), { signal: events.signal });
    host.addEventListener('fve-mermaid-render', () => render(true), { signal: events.signal });
    host.addEventListener('datastar-scope-children', () => render(), { signal: events.signal });
    render();
    cleanup(() => { disposed = true; generation++; events.abort(); observer.disconnect(); appearance.disconnect(); visibility.disconnect(); });
  },
});
