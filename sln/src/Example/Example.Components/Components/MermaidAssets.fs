namespace Example.Components

open System.Text.Json
open FSharp.ViewEngine
open type Html

module internal MermaidAssets =
    let renderWithNonce (scriptPath:string) (nonce:string option) =
        let source =
            """
(() => {
if (window.fsharpDocsMermaid) return;
window.fsharpDocsMermaid = {
  source: __MERMAID_SCRIPT__, nonce: __ASSET_NONCE__, security: 'antiscript', loading: null,
  loadScript(source) {
    return new Promise((resolve, reject) => {
      const script = document.createElement('script'); script.src = source; script.dataset.docsMermaidAsset = 'true';
      if (this.nonce) script.nonce = this.nonce;
      script.addEventListener('load', resolve, { once: true });
      script.addEventListener('error', () => reject(new Error(`Unable to load Mermaid asset: ${source}`)), { once: true });
      document.head.append(script);
    });
  },
  hasApi() { return typeof window.mermaid?.initialize === 'function' && typeof window.mermaid?.render === 'function'; },
  async ensure() {
    if (this.hasApi() || !this.source) return;
    if (!this.loading) this.loading = this.loadScript(this.source);
    await this.loading;
  }
};
})();
            """
                .Replace("__MERMAID_SCRIPT__", JsonSerializer.Serialize scriptPath)
                .Replace("__ASSET_NONCE__", nonce |> Option.map JsonSerializer.Serialize |> Option.defaultValue "null")
        script {
            match nonce with Some value -> _attr("nonce", value) | None -> ()
            raw source
        }

    let render scriptPath = renderWithNonce scriptPath None
