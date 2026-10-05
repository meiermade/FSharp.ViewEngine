namespace FSharp.ViewEngine.Components

open System.Text.Json
open FSharp.ViewEngine
open type Html

module internal MermaidAssets =
    let render (scriptPath:string) =
        let source =
            """
(() => {
if (window.renderMermaid) return;
window.fsharpDocsMermaid = window.fsharpDocsMermaid ?? {
  source: __MERMAID_SCRIPT__,
  nonce: __ASSET_NONCE__,
  loading: null,
  loadScript(source) {
    return new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = source;
      script.dataset.docsMermaidAsset = 'true';
      if (this.nonce) script.nonce = this.nonce;
      script.addEventListener('load', resolve, { once: true });
      script.addEventListener('error', () => reject(new Error(`Unable to load Mermaid asset: ${source}`)), { once: true });
      document.head.append(script);
    });
  },
  hasApi() {
    return typeof window.mermaid?.initialize === 'function' && typeof window.mermaid?.render === 'function';
  },
  async ensure() {
    if (this.hasApi() || !this.source) return;
    if (!this.loading) this.loading = this.loadScript(this.source);
    await this.loading;
  }
};
let mermaidRenderQueue = Promise.resolve();
let mermaidRenderId = 0;
const mermaidStatus = (role, message) => {
  const status = document.createElement('p');
  status.className = 'm-0 text-center text-sm leading-relaxed text-[var(--fve-muted-text)]';
  status.dataset.mermaidStatus = 'true';
  status.setAttribute('role', role);
  status.textContent = message;
  return status;
};
const setMermaidPending = node => {
  node.dataset.mermaidState = 'pending';
  delete node.dataset.mermaidRenderedSource;
  node.setAttribute('aria-busy', 'true');
  node.replaceChildren(mermaidStatus('status', 'Rendering diagram…'));
};
const setMermaidFailed = node => {
  node.dataset.mermaidState = 'failed';
  node.removeAttribute('aria-busy');
  node.replaceChildren(mermaidStatus('alert', 'Diagram unavailable.'));
};
window.renderMermaid = (el, pendingOnly = false) => {
  const render = async () => {
    const candidates = el?.matches?.('.mermaid') ? [el] : Array.from(el?.querySelectorAll?.('.mermaid') ?? []);
    const nodes = candidates.filter(node => !pendingOnly || node.dataset.mermaidState !== 'rendered' || node.dataset.mermaidRenderedSource !== (node.dataset.mermaidSource ?? '') || !node.querySelector('svg'));
    if (nodes.length === 0) return;
    for (const node of nodes) setMermaidPending(node);
    try {
      await window.fsharpDocsMermaid.ensure();
      if (!window.fsharpDocsMermaid.hasApi()) throw new Error('Mermaid is unavailable.');
      window.mermaid.initialize({ startOnLoad: false, theme: document.documentElement.classList.contains('dark') ? 'dark' : 'neutral', securityLevel: __SECURITY__, suppressErrorRendering: true });
    } catch {
      for (const node of nodes) if (node.isConnected) setMermaidFailed(node);
      return;
    }
    for (const node of nodes) {
      if (!node.isConnected) continue;
      const source = node.dataset.mermaidSource ?? '';
      try {
        const id = `fsharp-docs-mermaid-${++mermaidRenderId}`;
        const { svg, bindFunctions } = await window.mermaid.render(id, source);
        if (!node.isConnected) continue;
        if ((node.dataset.mermaidSource ?? '') !== source) {
          setMermaidPending(node);
          continue;
        }
        node.innerHTML = svg;
        bindFunctions?.(node);
        node.dataset.mermaidState = 'rendered';
        node.dataset.mermaidRenderedSource = source;
        node.removeAttribute('aria-busy');
      } catch {
        if (!node.isConnected) continue;
        if ((node.dataset.mermaidSource ?? '') !== source) setMermaidPending(node);
        else setMermaidFailed(node);
      }
    }
  };
  mermaidRenderQueue = mermaidRenderQueue.then(render, render);
  return mermaidRenderQueue;
};
window.addEventListener('fsharpdocs:colormode', () => window.renderMermaid?.(document));

})();
            """
                .Replace("__MERMAID_SCRIPT__", JsonSerializer.Serialize scriptPath)
                .Replace("__SECURITY__", "\"antiscript\"")
                .Replace("__ASSET_NONCE__", "null")
        script { raw source }
