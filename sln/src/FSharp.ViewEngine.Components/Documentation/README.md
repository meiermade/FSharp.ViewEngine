# FSharp.ViewEngine.Components.Documentation

Composable documentation components for [FSharp.ViewEngine](https://www.nuget.org/packages/FSharp.ViewEngine).

Use one toolkit to build:

- product guides and conceptual articles
- Stripe- or Privy-style API references
- executable software specifications
- wide product and architecture canvases
- component galleries and internal documentation

The package supplies consistent mechanics and presentation without prescribing a consumer's information architecture, product workflows, or diagram depth.

## Installation

```shell
dotnet add package FSharp.ViewEngine.Components
```

```shell
dotnet paket add FSharp.ViewEngine.Components
```

Documentation is part of the single Components package, not a separate assembly or release. Components targets `net8.0` for .NET 8, .NET 9, and .NET 10 consumers and declares its minimum engine dependency: `FSharp.ViewEngine ← FSharp.ViewEngine.Components`. Compatibility still requires packaged-consumer verification for the exact release candidate.

## Tailwind CSS 4

Documentation presentation is consumer-compiled. Components includes `FSharp.ViewEngine.Components.tailwind.css` and the optional `Documentation/Documentation.tailwind.css` under `contentFiles/any/any`; copy them into the application CSS source tree, retaining that directory structure, and import them after Tailwind:

```css
@import "tailwindcss";
@import "./FSharp.ViewEngine.Components.tailwind.css";
@import "./Documentation/Documentation.tailwind.css";
@source "./src/**/*.fs";
```

The Components manifest supplies shared utilities and semantic component variables. The Docs manifest supplies the current documentation theme, layout, responsive, Prism, and specialized selectors. Later renderer migrations may replace applicable selectors with renderer-owned utilities without changing this two-manifest consumer boundary.

Compile that source to a host-owned stylesheet and expose it through `DocsAssets.productStylesheets`. `DocsAssets.defaults` expects `/css/compiled.css`; use the actual output path when the host chooses another name:

```fsharp
let assets =
    { DocsAssets.defaults with
        productStylesheets = [ "/css/docs.css" ] }
```

The package does not inject a fallback `<style>` element. Omitting either manifest leaves its corresponding presentation unavailable rather than hiding an incomplete installation behind embedded global CSS.

### Migrating from embedded Docs styles

The consumer-compiled Tailwind contract intentionally replaces the former self-contained styling behavior:

1. Add Tailwind CSS 4 to the consuming application build.
2. Copy and import both package manifests in the order shown above.
3. Serve the compiled stylesheet from the path configured in `productStylesheets`.
4. Keep consumer overrides after the package imports so semantic variables and application-specific rules remain consumer-owned.
5. Remove CSP allowances, hashes, or nonce handling that existed only for the former package-generated `<style>` element. Continue supplying `DocsAssets.nonce` when the generated inline scripts require it.

## Migration from the retired Docs package

Replace the `FSharp.ViewEngine.Docs` package/project reference with `FSharp.ViewEngine.Components`; change `open FSharp.ViewEngine.Docs` to `open FSharp.ViewEngine.Components.Documentation`. Replace the old stylesheet import with `Documentation/Documentation.tailwind.css`. Use `DocsSite`, `Document`, `DocumentationPage`, `DocumentationSection`, `CodeBlock`, `Callout`, and `Mermaid`. Documentation content is typed semantic HTML; there are no legacy block, inline, or `docs*` builder APIs and no second implementation.

Pinned historical Docs versions remain untouched. NuGet deprecation with Components as the alternative happens only after the replacement release and migration are verified; this source migration is not a claim that deprecation has occurred.

Ordinary product pages need only the shared manifest. Referencing Components does not inject Documentation navigation, Prism, Mermaid, or viewer scripts; the host opts into documentation rendering and its assets explicitly.

### Browser, Phone, and Fixture App mode

`Browser` and `Phone` are shared Primitives. They render independently, and `withAppMode` opts a named frame into the viewport viewer without making Documentation a dependency. `Fixture` is the Documentation composition that supplies Previous/Next workflow destinations and independent review states; Next never cycles through validation or error views. Products retain their content, routes, and state semantics.

```fsharp
open FSharp.ViewEngine.Components.Primitives
open FSharp.ViewEngine.Components.Documentation

let checkout =
    Browser.create shippingScreen
    |> Browser.withAddress "https://shop.example.test/checkout/shipping"
    |> Browser.withAppMode "checkout-shipping" "Shipping address"
    |> Browser.render
    |> Fixture.create "checkout-shipping"
    |> Fixture.withPrevious (FixtureLink.create "Cart" "/checkout/cart")
    |> Fixture.withNext (FixtureLink.create "Payment" "/checkout/payment")
    |> Fixture.withStates [
        FixtureState.create "Ready" "/checkout/shipping" |> FixtureState.current
        FixtureState.create "Address error" "/checkout/shipping?state=error" ]
    |> Fixture.render

let head = Browser.script "/scripts/fve-app-mode.js"
```

Copy `app-mode.js` from the package root to the URL supplied to `Browser.script`, include it once in the host document head, and import the shared `FSharp.ViewEngine.Components.tailwind.css` manifest. The runtime uses normal document navigation and history; it does not use the browser Fullscreen API or replace the document body.

## Builder API

Build a site and page from immutable typed values; section content is ordinary `HtmlElement` content:

```fsharp
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Documentation
open type Html

type Destination = Home | Installation | RenderReference

let navigation =
    [ Nav.page "home" "Overview" "/" Home
      Nav.group "guides" "Guides" true [
          Nav.page "installation" "Installation" "/installation" Installation ]
      Nav.page "render" "Render.toString" "/api/render" RenderReference ]

let site =
    DocsSite.create "Example Docs" "home"
    |> DocsSite.withNavigation navigation
    |> DocsSite.withDescription "Documentation for Example."
    |> DocsSite.withTheme DocsTheme.sky

let installation =
    DocumentationPage.create "installation" "Installation"
    |> DocumentationPage.withDescription "Install the package."
    |> DocumentationPage.withSections [
        DocumentationSection.create "package" "Package" [
            CodeBlock.create "shell" "dotnet add package Example" |> CodeBlock.render ] ]

let html =
    Document.create site installation
    |> Document.render
    |> Render.toHtmlDocString
```

`Document.create` requires both a site and page. `Document.withBreadcrumbs` and `Document.withSideNavItems` are optional overrides for hosts that compose non-registry navigation.

## Layouts

### Articles

`DocumentationPage.create` defaults to a readable article with an on-this-page rail:

```fsharp
let article =
    DocumentationPage.create "guide" "Getting started"
    |> DocumentationPage.withDescription "Build your first view."
    |> DocumentationPage.withSections [
        DocumentationSection.create "create" "Create a view" [
            p { _class "spec-paragraph"; "Compose typed elements with computation expressions." }
            ul { _class "spec-bullets list-disc"; li { "Open FSharp.ViewEngine" }; li { "Open the HTML builders" } } ] ]
```

Add explicit previous and next destinations when the intended reading order differs from the sidebar. The pager uses the same Docs-managed navigation lifecycle as the side navigation:

```fsharp
let guidedArticle =
    article
    |> DocumentationPage.withPager (
        DocsPager.create
            (Some(DocsPageLink.create "Introduction" "/"))
            (Some(DocsPageLink.create "Usage" "/usage")))
```

Use `None` at either end of a sequence.

### API references

`DocumentationPage.withLayout Reference` adds a dedicated right rail for request and response examples:

```fsharp
let requestRail =
    div {
        ApiReference.codeExample "Render a view" "fsharp" "div { \"Saved\" } |> Render.toString"
        ApiReference.responseExample "200" "html" "<div>Saved</div>"
    }

let reference =
    DocumentationPage.create "render" "Render.toString"
    |> DocumentationPage.withDescription "Serializes an HTML element."
    |> DocumentationPage.withLayout Reference
    |> DocumentationPage.withRightRail (CustomRail requestRail)
    |> DocumentationPage.withSections [
        DocumentationSection.create "signature" "Signature" [
            Endpoint.create POST "/v1/render"
            |> Endpoint.withDescription "Renders an element."
            |> Endpoint.render ]
        DocumentationSection.create "parameters" "Parameters" [
            Parameter.create "element" "HtmlElement" Body
            |> Parameter.required
            |> Parameter.withDescription "The element to serialize."
            |> Parameter.render ] ]
```

### Canvases

`DocumentationPage.withLayout Canvas` provides the widest content surface with a visible semantic heading. When a product frame or architecture diagram already carries the visible title, add `DocumentationPage.withHiddenHeading` to retain an accessible `<h1>` without duplicating it visually:

```fsharp
let canvas =
    DocumentationPage.create "workflow" "Create an item" |> DocumentationPage.withDescription "Create an item from an empty state." |> DocumentationPage.withLayout Canvas |> DocumentationPage.withRightRail NoRail |> DocumentationPage.withHiddenHeading |> DocumentationPage.withSections [
        DocumentationSection.create "wireframe" "Wireframe" [
            Browser.create productUi
            |> Browser.withAddress "https://example.test/items/new"
            |> Browser.render ] ]
```

## Content builders

- `DocumentationSection.create` accepts semantic `HtmlElement` content directly.
- Use the ordinary FSharp.ViewEngine HTML builders for paragraphs, lists, tables, links, and inline content.
- `CodeBlock.create language source |> CodeBlock.render` renders copyable, highlighted source.
- `Callout.create label content |> Callout.render` owns the callout treatment while callers own semantic content.
- `Mermaid.create source |> Mermaid.render`, `Mermaid.withC4`, and `SequenceDiagram` render trusted diagrams.
- `Example.codeFirst`, `Example.previewFirst`, and `Example.gallery` keep an exact source, preview, and Copy action together.
- `DocumentationPage.withPager` accepts a `DocsPager` when reading order differs from navigation.

Mermaid components encode trusted source outside visible content, show an accessible rendering status while the lazy asset loads, and show `Diagram unavailable.` as an alert if rendering fails. Direct loads, Docs navigation, and color-mode changes share this lifecycle.

## Interactive components

```fsharp
let states =
    [ Tab.create "empty" "Empty" (div { "No items" })
      Tab.create "ready" "Ready" (div { "Items" }) ]
    |> Tabs.create "item-states" "Item states"
    |> Tabs.withVariant TabsVariant.Underlined
    |> Tabs.render

let framed =
    Browser.create states
    |> Browser.withAddress "https://example.test/items"
    |> Browser.render
```

`Tabs` provides tablist, tab, and tabpanel semantics with click and arrow-key interactions.

### Component galleries

Use `DocumentationPage.withLayout Gallery` and `DocumentationPage.withRightRail NoRail` for named example collections without article section headings or a table-of-contents rail. Pair it with `Example.gallery`, whose heading and Preview / Code / Copy controls sit above the example frame. Put complete example values and imports in the copied source; keep installation and shared conventions in separate guides.

```fsharp
let buttons =
    DocumentationPage.create "buttons" "Buttons" |> DocumentationPage.withLayout Gallery |> DocumentationPage.withRightRail NoRail |> DocumentationPage.withSections [
        DocumentationSection.create "primary" "Primary" [
            Example.gallery "primary-button" "Primary" "fsharp" source preview ] ]
```

### Code and preview examples

Use `Example.codeFirst` for a source-first developer example. `Example.previewFirst` is available where the rendered result should lead. Each example has independent accessible tab state and keyboard navigation:

```fsharp
let example =
    Example.codeFirst
        "notice-example"
        "Notice"
        "fsharp"
        "div { _class \"notice\"; \"Saved\" }"
        (div { _class "notice"; "Saved" })
```

Each example renders a direct Copy action for its exact source alongside independent Code and Preview tabs. Keep installation commands, isolated signatures, configuration, and migration fragments as `CodeBlock` values.

For reusable component catalogs, use `DocumentationPage.withLayout Gallery` with `Example.gallery` so the exact preview, source, and Copy control stay together. `VersionView.selector` remains opt-in for consumers publishing multiple documentation versions.

API references can grow from `Endpoint` and `Parameter` to `Operation.create method path |> Operation.with... |> Operation.render`, which supports authentication, path/query/header/body parameter locations, defaults, enum/example values, responses, error models, idempotency guidance, API versions, and deprecation metadata. Use `Response.create` and `Error.create` for the associated models; this keeps all optional policy visible at the call site instead of in a positional argument list.

## Search, metadata, and validation

Build a dependency-free local index from page models and assign it to `DocsSite.search`:

```fsharp
let search =
    [ DocsSearchEntry.create "/installation" installation [ "package"; "NuGet" ] ]
    |> DocsSearch.index

let searchableSite = { site with search = search }
```

The built-in accessible search dialog opens with `Ctrl+K` or `Cmd+K`, filters page titles, descriptions, headings, and consumer keywords, and links directly to matching sections.

Use `DocsPageMetadata` and `DocumentationPage.withMetadata` for browser titles, canonical overrides, robots directives, social images, version/deprecation badges, last-updated dates, and edit-source links. Register canonical pages and aliases with `DocsRegisteredPage.create`, then use `DocsRegistry.validate` to check navigation reachability, route/alias collisions, pager targets, page metadata, and section structure.

## Assets and themes

Docs component CSS is compiled by the consuming Tailwind build. `DocsAssets.defaults` references conventional consumer-hosted paths for the compiled stylesheet, Prism, Mermaid, and Datastar. Override or disable them as needed:

```fsharp
let assets =
    { DocsAssets.defaults with
        productStylesheets = [ "/css/docs.css" ]
        prismStylesheet = None
        prismScripts = []
        mermaidScript = Some "/scripts/mermaid.min.js"
        datastarScript = Some "/scripts/datastar.js"
        mermaidSecurityLevel = "strict"
        nonce = Some requestNonce }
```

The Docs Tailwind manifest includes coordinated light and dark Prism token palettes, so code samples follow the active documentation color mode without a separate default theme request. Set `prismStylesheet` when a consumer-owned Prism theme should override that palette. Prism scripts remain lazy and load only when code highlighting is requested. Mermaid assets are emitted only for typed diagram blocks. Custom HTML remains consumer-owned and should provide its own page-specific assets through `additionalHead`. Set `nonce` from each HTTP response when enforcing a nonce-based Content Security Policy for the generated inline scripts.

Built-in accent themes include `DocsTheme.amber`, `DocsTheme.sky`, and `DocsTheme.emerald`. `defaultColorMode` accepts `DocsColorMode.System`, `Light`, or `Dark`; the built-in icon-triggered `DropdownMenu` persists the visitor's choice and responds to operating-system changes while in System mode. System, Light, and Dark use checked radio menu items. App mode relocates that same control into its inverse-themed dock, where both trigger and popup inherit the dock's standard Components tokens; no separate theme preference or popup styling is needed. Article tables of contents use the nested documentation viewport for active-section tracking, expose `aria-current="location"` on the current section, and become a compact native disclosure below the page introduction on narrower screens. Use `DocsRepository.github` for the compact GitHub repository action or `DocsRepository.link` for another repository host.

The Docs Tailwind manifest exposes `--docs-font-sans` and `--docs-font-mono`. They prefer Noto Sans and Noto Sans Mono with system fallbacks, but the package does not ship font binaries or request Google-hosted assets. Hosts that want the preferred appearance can self-host the variable WOFF2 files under their own CSP:

```css
@font-face {
  font-family: "Noto Sans";
  font-style: normal;
  font-weight: 100 900;
  font-display: swap;
  src: url("/fonts/noto-sans-latin.woff2") format("woff2");
}

@font-face {
  font-family: "Noto Sans";
  font-style: italic;
  font-weight: 100 900;
  font-display: swap;
  src: url("/fonts/noto-sans-latin-italic.woff2") format("woff2");
}

@font-face {
  font-family: "Noto Sans Mono";
  font-style: normal;
  font-weight: 100 900;
  font-display: swap;
  src: url("/fonts/noto-sans-mono-latin.woff2") format("woff2");
}
```

Override the variables when another consumer-owned family is preferred. The semantic defaults are exposed as `--docs-text-ancillary` (12px equivalent), `--docs-text-ui` (14px), `--docs-text-reading` (16px), and `--docs-text-code` (14px). Docs-managed Datastar navigation restores color mode, scrolls documentation content to the top, and reruns Prism and Mermaid after morphing. All JavaScript string configuration is serialized before insertion into scripts.

## Structural validation

Navigation validation checks IDs, labels, paths, empty groups, and duplicate page routes without prescribing hierarchy. `DocsPage.validate` checks page and section structure. `DirectedGraph.validate` supports consumer-defined workflow, architecture, or navigation graphs without imposing domain depth.

Typed targets preserve consumer destination identity while encoding query parameters and fragments:

```fsharp
let href =
    Target.create Installation
    |> Target.withQuery "returnTo" "/"
    |> Target.withFragment "package"
    |> Target.href (function Home -> "/" | Installation -> "/installation" | RenderReference -> "/api/render")
```

## Local review

This repository's `sln/src/Docs` application consumes the package directly and includes article, API-reference, canvas, component-lab, and executable-specification examples:

```shell
cd sln
./fake.sh WatchDocs
```
