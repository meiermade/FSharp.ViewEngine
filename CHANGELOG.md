# Changelog

See [GitHub releases](https://github.com/meiermade/FSharp.ViewEngine/releases) for earlier versions.

## Unreleased

- Install controls with `fve add link button`. `fveLink` uses Datastar’s `@get()` by default; use `_native true` for native navigation. The server owns URL changes. See the Giraffe guide for `pushUrl` and Back/Forward.
- Use `fve compile` and `fve watch` for web CSS and JS. Update the existing manifest to schema 2 and add the `frontend` paths from a freshly initialized `fve.json` (use `frontend: null` for scripts). Run `fve init` again to add missing npm dependencies and the generated imports entry. Remove duplicate manual component imports; keep your app code, CSS, and local component edits.
- Replace `fve build --open` with `fve run page.fsx`. The CLI prints the output paths instead of launching a browser.
- `fveCodeBlock` supports optional filenames, source downloads, line numbers, and highlighted lines. Copy and Download exclude the visual annotations.
- Code Block, Mermaid, and Chart now bundle Prism, Mermaid, and ECharts. Update installed source with `fve add … --overwrite`, run `npm install` for web projects, and remove `CodeBlock.assets`/`assetsWithNonce` and `Mermaid.assets`/`assetsWithNonce` calls. Remove any unused vendored Prism, Mermaid, and ECharts files. Standalone scripts resolve these packages from the CLI cache.
- Tailwind Plus Elements remains opt-in: install it with npm and import it in your app entry. FVE only provides the HTML helpers.
