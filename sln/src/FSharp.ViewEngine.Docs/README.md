# FSharp.ViewEngine.Docs

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
dotnet add package FSharp.ViewEngine.Docs
```

```shell
dotnet paket add FSharp.ViewEngine.Docs
```

The package targets `net8.0` and is compatible with .NET 8, .NET 9, and .NET 10 applications.

## Builder API

Open the namespace once. Package builders use the `docs` prefix for IntelliSense discovery and to avoid collisions with HTML builders:

```fsharp
open FSharp.ViewEngine
open FSharp.ViewEngine.Docs
open type Html

type Destination =
    | Home
    | Installation
    | RenderReference

let navigation =
    [ docsNavPage "home" "Overview" "/" Home
      docsNavGroup "guides" "Guides" true [
          docsNavPage "installation" "Installation" "/installation" Installation ]
      docsNavPage "render" "Render.toString" "/api/render" RenderReference ]

let site: DocsSite<Destination> =
    { name = "Example Docs"
      baseUrl = Some "https://docs.example.com"
      description = Some "Documentation for Example."
      repository = Some(DocsRepository.github "https://github.com/example/project")
      brandMark = span { _ariaHidden "true"; "E" }
      homeId = "home"
      navigation = navigation
      storageKey = "example-docs-navigation"
      defaultColorMode = DocsColorMode.System
      theme = DocsTheme.sky
      assets = DocsAssets.defaults
      search = [] }

let installation =
    docsArticle "installation" "Installation" "Install the package." [
        docsSection "package" "Package" [
            docsCode "shell" "dotnet add package Example" ] ]

let html = installation |> docsDocument site |> Render.toHtmlDocString
```

## Layouts

### Articles

`docsArticle` renders a readable content column with an on-this-page rail:

```fsharp
let article =
    docsArticle "guide" "Getting started" "Build your first view." [
        docsSection "create" "Create a view" [
            docsParagraph "Compose typed elements with computation expressions."
            docsBullets [ "Open FSharp.ViewEngine"; "Open the HTML builders" ] ] ]
```

Add explicit previous and next destinations when the intended reading order differs from the sidebar. The pager uses the same Docs-managed navigation lifecycle as the side navigation:

```fsharp
let guidedArticle =
    article
    |> docsWithPager (
        docsPager
            (Some(docsPageLink "Introduction" "/"))
            (Some(docsPageLink "Usage" "/usage")))
```

Use `None` at either end of a sequence.

### API references

`docsReference` adds a dedicated right rail for request and response examples:

```fsharp
let requestRail =
    div {
        docsCodeExample "Render a view" "fsharp" "div { \"Saved\" } |> Render.toString"
        docsResponseExample "200" "html" "<div>Saved</div>"
    }

let reference =
    docsReference "render" "Render.toString" "Serializes an HTML element." [
        docsSection "signature" "Signature" [
            docsCustom (docsApiEndpoint POST "/v1/render" "Renders an element.") ]
        docsSection "parameters" "Parameters" [
            docsCustom (docsParameters [
                docsParameter "element" "HtmlElement" true "The element to serialize." ]) ] ]
        (docsRail requestRail)
```

### Canvases

`docsCanvas` provides the widest content surface with a visible semantic heading. When a product frame or architecture diagram already carries the visible title, use `docsCanvasWithHiddenHeading` to retain an accessible `<h1>` without duplicating it visually:

```fsharp
let canvas =
    docsCanvasWithHiddenHeading "workflow" "Create an item" "Create an item from an empty state." [
        docsSection "wireframe" "Wireframe" [
            docsCustom (docsBrowserFrame "https://example.test/items/new" productUi) ] ]
```

## Content builders

- `docsSection` and `docsSubsection`
- `docsParagraph`, `docsRichParagraph`, `docsBullets`, `docsRichBullets`, and `docsOrderedItems`
- `docsText`, `docsInlineCode`, `docsLink`, `docsStrong`, and `docsEmphasis`
- `docsTable` and `docsRichTable`
- `docsCode`
- `docsCallout` and `docsRichCallout`
- `docsDiagram`, `docsC4Diagram`, and `docsSequence`
- `docsCustom`
- `docsPageLink`, `docsPager`, and `docsWithPager`

`docsDiagram` and `docsC4Diagram` accept trusted Mermaid source. `docsSequence` accepts a diagram constructed with the validated `SequenceDiagram` DSL.

## Interactive components

```fsharp
let states =
    docsStateTabs "item-states" "Item states" [
        { id = "empty"; label = "Empty"; content = div { "No items" } }
        { id = "ready"; label = "Ready"; content = div { "Items" } } ]

let framed = docsBrowserFrame "https://example.test/items" states
```

State tabs include tablist, tab, and tabpanel semantics with click and arrow-key interactions.

### Code and preview examples

Use `docsExample` for a source-first developer example. `docsExampleCodeFirst` remains an equivalent explicit alias. Each example has independent accessible tab state and keyboard navigation. Opening Code reruns Prism; opening Preview rerenders Mermaid after the panel has measurable layout:

```fsharp
let example =
    docsExample
        "notice-example"
        "Notice"
        "fsharp"
        "div { _class \"notice\"; \"Saved\" }"
        (div { _class "notice"; "Saved" })
```

Use examples for renderable output. Keep installation commands, isolated signatures, configuration, and migration fragments as `docsCode` blocks.

For reusable component catalogs, `DocsStory` records exact source/preview pairs plus optional viewport, theme, and state metadata. `docsStoryCatalog` renders those stories without taking ownership of product appearance. `docsVersionSelector` is opt-in for consumers publishing multiple documentation versions.

API references can grow from the compact endpoint/parameter builders to `docsApiOperation`, which supports authentication, path/query/header/body parameter locations, defaults, enum/example values, responses, error models, idempotency guidance, API versions, and deprecation metadata.

## Search, metadata, and validation

Build a dependency-free local index from page models and assign it to `DocsSite.search`:

```fsharp
let search =
    [ docsSearchEntry "/installation" installation [ "package"; "NuGet" ] ]
    |> DocsSearch.index

let searchableSite = { site with search = search }
```

The built-in accessible search dialog opens with `Ctrl+K` or `Cmd+K`, filters page titles, descriptions, headings, and consumer keywords, and links directly to matching sections.

Use `DocsPageMetadata` and `docsWithMetadata` for browser titles, canonical overrides, robots directives, social images, version/deprecation badges, last-updated dates, and edit-source links. Register canonical pages and aliases with `docsRegisteredPage`, then use `DocsRegistry.validate` to check navigation reachability, route/alias collisions, pager targets, page metadata, and section structure.

## Assets and themes

Default component CSS is embedded in the rendered document. `DocsAssets.defaults` references conventional consumer-hosted paths for Prism, Mermaid, Datastar, and a product stylesheet. Override or disable them as needed:

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

Prism assets are emitted only for typed `docsCode` blocks and Mermaid assets only for typed diagram blocks. Custom HTML remains consumer-owned and should provide its own page-specific assets through `additionalHead`. Set `nonce` from each HTTP response when enforcing a nonce-based Content Security Policy.

Built-in accent themes include `DocsTheme.amber`, `DocsTheme.sky`, and `DocsTheme.emerald`. `defaultColorMode` accepts `DocsColorMode.System`, `Light`, or `Dark`; the built-in accessible selector persists the visitor's choice and responds to operating-system changes while in System mode. Article tables of contents use the nested documentation viewport for active-section tracking, expose `aria-current="location"` on the current section, and become a compact native disclosure below the page introduction on narrower screens. Use `DocsRepository.github` for the compact GitHub repository action or `DocsRepository.link` for another repository host.

The embedded styles expose `--docs-font-sans` and `--docs-font-mono`. They prefer Noto Sans and Noto Sans Mono with system fallbacks; hosts may self-host those fonts or override the variables. Docs-managed Datastar navigation restores color mode, scrolls documentation content to the top, and reruns Prism and Mermaid after morphing. All JavaScript string configuration is serialized before insertion into scripts.

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
