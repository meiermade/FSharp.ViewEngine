namespace FSharp.ViewEngine.Components

open System.Text.Json
open FSharp.ViewEngine
open type Html

module internal CodeAssets =
    let renderWithNonce (stylesheet:string option) (scripts:string list) (nonce:string option) =
        let source =
            """
(() => {
if (window.renderCode && window.fsharpDocsCopy) return;
const markDocsCodeUnloading = () => { window.fsharpDocsCode.unloading = true; };
window.fsharpDocsCode = window.fsharpDocsCode ?? {
  stylesheet: __PRISM_STYLESHEET__,
  scripts: __PRISM_SCRIPTS__,
  nonce: __ASSET_NONCE__,
  loading: null,
  unloading: false,
  abandonOrReject(resolve, reject, source) {
    if (this.unloading) resolve();
    else reject(new Error(`Unable to load Prism asset: ${source}`));
  },
  loadStylesheet(source) {
    const href = new URL(source, document.baseURI).href;
    const existing = Array.from(document.querySelectorAll('link[rel="stylesheet"]')).find(link => link.href === href);
    if (existing?.sheet) return Promise.resolve();
    return new Promise((resolve, reject) => {
      const link = existing ?? document.createElement('link');
      link.rel = 'stylesheet';
      link.href = source;
      link.dataset.docsPrismAsset = 'true';
      link.addEventListener('load', resolve, { once: true });
      link.addEventListener('error', () => this.abandonOrReject(resolve, reject, source), { once: true });
      if (!existing) document.head.append(link);
    });
  },
  loadScript(source) {
    return new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = source;
      script.dataset.docsPrismAsset = 'true';
      if (this.nonce) script.nonce = this.nonce;
      script.addEventListener('load', resolve, { once: true });
      script.addEventListener('error', () => this.abandonOrReject(resolve, reject, source), { once: true });
      document.head.append(script);
    });
  },
  async ensure() {
    if (!this.loading) {
      this.unloading = false;
      window.addEventListener('beforeunload', markDocsCodeUnloading);
      this.loading = (async () => {
        if (this.stylesheet) await this.loadStylesheet(this.stylesheet);
        if (this.unloading || window.Prism?.languages?.fsharp) return;
        window.Prism = window.Prism || {};
        window.Prism.manual = true;
        for (const source of this.scripts) {
          if (this.unloading) return;
          await this.loadScript(source);
        }
      })().finally(() => window.removeEventListener('beforeunload', markDocsCodeUnloading));
    }
    await this.loading;
  },
  async render(el) {
    const root = el ?? document;
    if (!root.querySelector?.('code[class*="language-"]') && !root.matches?.('code[class*="language-"]')) return;
    await this.ensure();
    window.Prism?.highlightAllUnder?.(root);
  }
};
window.addEventListener('pagehide', markDocsCodeUnloading);
window.addEventListener('pageshow', () => { window.fsharpDocsCode.unloading = false; });
window.renderCode = el => window.fsharpDocsCode.render(el);
window.fsharpDocsCopy = async button => {
  const source = button.closest('[data-docs-copyable-code]')?.querySelector('[data-docs-copy-source]')?.textContent ?? '';
  const label = button.querySelector('[data-docs-copy-label]');
  window.clearTimeout(button.docsCopyReset);
  delete button.dataset.copied;
  delete button.dataset.copyError;
  try {
    await navigator.clipboard.writeText(source);
    if (label) label.textContent = 'Copied';
    button.title = 'Copied';
    button.dataset.copied = 'true';
  } catch {
    if (label) label.textContent = 'Copy failed';
    button.title = 'Copy failed';
    button.dataset.copyError = 'true';
  }
  button.docsCopyReset = window.setTimeout(() => {
    if (label) label.textContent = '';
    button.title = button.getAttribute('aria-label');
    delete button.dataset.copied;
    delete button.dataset.copyError;
  }, 1600);
};

})();
            """
                .Replace("__PRISM_STYLESHEET__", JsonSerializer.Serialize stylesheet)
                .Replace("__PRISM_SCRIPTS__", JsonSerializer.Serialize scripts)
                .Replace("__ASSET_NONCE__", nonce |> Option.map JsonSerializer.Serialize |> Option.defaultValue "null")
        script {
            match nonce with Some value -> _attr("nonce", value) | None -> ()
            raw source
        }

    let render stylesheet scripts = renderWithNonce stylesheet scripts None
