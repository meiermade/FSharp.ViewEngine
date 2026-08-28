namespace Docs.Pages

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Docs
open type Html

module Showcase =
    let private registration (id:string) (path:string) (aliases:string list) (navLabel:string) (title:string) : DocPage =
        { id = id
          path = path
          aliases = aliases
          navLabel = navLabel
          category = "FSharp.ViewEngine.Docs"
          title = title
          browserTitle = $"{title} · FSharp.ViewEngine.Docs"
          nodes = [] }

    let overviewRegistration =
        registration "docs-overview" "/docs" [] "Overview" "FSharp.ViewEngine.Docs"

    let layoutsRegistration =
        registration "docs-layouts" "/docs/components/layouts" [ "/docs/components"; "/docs-components" ] "Layouts" "Layouts"

    let contentRegistration =
        registration "docs-content" "/docs/components/content" [] "Content" "Content"

    let navigationRegistration =
        registration "docs-navigation" "/docs/components/navigation" [] "Navigation" "Navigation"

    let interactiveRegistration =
        registration "docs-interactive" "/docs/components/interactive-examples" [] "Interactive examples" "Interactive examples"

    let apiComponentsRegistration =
        registration "docs-api-components" "/docs/components/api-reference" [] "API reference" "API reference components"

    let diagramsRegistration =
        registration "docs-diagrams" "/docs/components/diagrams" [] "Diagrams" "Diagrams"

    let documentationSiteRegistration =
        registration "docs-page-documentation-site" "/docs/page-examples/documentation-site" [] "Documentation site" "Documentation site"

    let apiPageExampleRegistration =
        registration
            "docs-page-api-reference"
            "/docs/page-examples/api-reference"
            [ "/docs/examples/api-reference"; "/api-reference/render-to-string" ]
            "API reference"
            "API reference page"

    let specificationPageExampleRegistration =
        registration
            "docs-page-executable-specification"
            "/docs/page-examples/executable-specification"
            [ "/docs/examples/executable-specification"; "/specification/render-a-view" ]
            "Executable specification"
            "Executable specification page"

    let componentRegistrations =
        [ layoutsRegistration
          contentRegistration
          navigationRegistration
          interactiveRegistration
          apiComponentsRegistration
          diagramsRegistration ]

    let pageExampleRegistrations =
        [ documentationSiteRegistration
          apiPageExampleRegistration
          specificationPageExampleRegistration ]

    let private previewSurface (content:HtmlElement) =
        div {
            _data("example-surface", "true")
            _class "docs-showcase-surface"
            content
        }

    let private sourceText =
        lazy (SourceRegion.readEmbedded typeof<DocPage>.Assembly "Docs.Pages.Showcase.fs")

    let sourceFor id = SourceRegion.extract id sourceText.Value

    let private componentExample (id:string) (label:string) (description:string) (preview:HtmlElement) =
        docsSection id label [
            docsParagraph description
            docsCustom (docsExample $"docs-{id}-example" label "fsharp" (sourceFor id) preview) ]

    let private catalogLink (href:string) (eyebrow:string) (title:string) (description:string) =
        a {
            _href href
            _class "docs-catalog-card"
            span { _class "docs-catalog-eyebrow"; eyebrow }
            strong { title }
            span { _class "docs-catalog-description"; description }
            span { _class "docs-catalog-action"; "Browse "; raw "&rarr;" }
        }

    let private miniatureArticle () =
        previewSurface (
            div {
                _class "docs-mini-shell"
                aside {
                    div { _class "docs-mini-brand"; "Acme Docs" }
                    div { _class "docs-mini-nav-active"; "Getting started" }
                    div { "Installation" }
                    div { "Configuration" }
                }
                div {
                    _class "docs-mini-main"
                    div { _class "docs-mini-breadcrumb"; "Guides / Getting started" }
                    h3 { "Build your first integration" }
                    p { "Compose a focused guide with navigation and an on-this-page rail." }
                    div { _class "docs-mini-code"; "dotnet add package Acme" }
                }
                nav {
                    _ariaLabel "Example table of contents"
                    small { "ON THIS PAGE" }
                    div { "Install" }
                    div { "Configure" }
                }
            })

    let private miniatureReference () =
        previewSurface (
            div {
                _class "docs-showcase-reference"
                div {
                    h3 { "Create a customer" }
                    docsApiEndpoint POST "/v1/customers" "Creates a customer and returns its identifier."
                    div { _style "margin-top:1rem"; docsParameters [ docsParameter "email" "string" true "Customer email address." ] }
                }
                div {
                    docsCodeExample "Request" "curl" "curl -X POST https://api.example.test/v1/customers"
                    docsResponseExample "201" "json" "{ \"id\": \"cus_123\" }"
                }
            })

    let private exampleSite : DocsSite<string> =
        { name = "Acme Docs"
          baseUrl = Some "https://docs.example.test"
          description = Some "Example documentation"
          repository = None
          brandMark = span { "AC" }
          homeId = "overview"
          navigation =
            [ docsNavGroup "guides" "Guides" true [
                docsNavPage "overview" "Overview" "/" "/"
                docsNavPage "guide" "Getting started" "/guide" "/guide" ] ]
          storageKey = "fsharp-view-engine-docs-example"
          defaultColorMode = DocsColorMode.System
          theme = DocsTheme.sky
          assets = { DocsAssets.defaults with productStylesheets = [ "/css/output.css" ] }
          search = [] }

    let private previewDocuments = System.Collections.Generic.Dictionary<string, string>()

    let private isolatedDocument (title:string) (canonicalUrl:string) (html:string) =
        if not (html.TrimStart().StartsWith("<!DOCTYPE html>", System.StringComparison.OrdinalIgnoreCase)) then
            invalidArg (nameof html) "Isolated previews must be complete HTML documents. Render component fragments directly in the host preview."

        let token =
            title.ToLowerInvariant()
            |> Seq.map (fun character -> if System.Char.IsLetterOrDigit character then character else '-')
            |> Seq.toArray
            |> System.String
        let previewPath = $"/docs/previews/{token}"
        previewDocuments[previewPath] <- html
        previewSurface (
            docsBrowserFrame canonicalUrl (
                iframe {
                    _class "docs-isolated-document"
                    _title title
                    _data("docs-preview-src", previewPath)
                }))

    let private isolatedPage title canonicalUrl page =
        page
        |> docsDocument exampleSite
        |> Render.toHtmlDocString
        |> isolatedDocument title canonicalUrl

    let private productView (instanceId:string) (state:string) =
        let hasValidation = state = "validation"
        let stateId = if hasValidation then "validation" else "ready"
        let suffix = $"{instanceId}-{stateId}"
        let inputBorder = if hasValidation then "#dc2626" else "#cbd5e1"
        div {
            _class "docs-product-screen"
            div {
                _class "docs-product-header"
                strong { "View Studio" }
                span { "FS" }
            }
            div {
                _class "docs-product-content"
                small { "NEW VIEW" }
                h3 { "Render your first component" }
                p { "Name the view and choose the output used by the application." }
                div {
                    _class "docs-product-card"
                    label { _for $"view-name-{suffix}"; "View name" }
                    input {
                        _id $"view-name-{suffix}"
                        _value (if hasValidation then "" else "accountSummary")
                        _placeholder "e.g. accountSummary"
                        _style $"border-color:{inputBorder}"
                    }
                    if hasValidation then p { _class "docs-product-error"; "Enter a view name." }
                    div {
                        _class "docs-product-actions"
                        button { _type "button"; "Cancel" }
                        button { _type "button"; _class "docs-product-primary"; "Create view" }
                    }
                }
            }
        }

    let private productScreen state =
        docsBrowserFrame "https://example.test/views/new" (productView "workflow" state)

    let private sequence () =
        let developer = SequenceDiagram.participant "Developer" "Developer"
        let engine = SequenceDiagram.participant "Engine" "View engine"
        let output = SequenceDiagram.participant "Output" "HTML output"
        SequenceDiagram.sequence [ developer; engine; output ] [
            SequenceDiagram.call developer engine "Build HtmlElement"
            SequenceDiagram.call developer engine "Render.toString element"
            SequenceDiagram.call engine output "Encode and serialize"
            SequenceDiagram.reply output developer "HTML string" ]

    let overviewPage =
        docsArticle overviewRegistration.id overviewRegistration.title "Composable layouts and components for product documentation, API references, executable specifications, and component review." [
            docsSection "purpose" "Built with the package" [
                docsParagraph "This documentation site is built with FSharp.ViewEngine.Docs. The shell, navigation, themes, examples, API components, browser frames, and diagrams shown here use the same public package available to consumers."
                docsParagraph "The package owns documentation mechanics and composition while each product retains its content, information architecture, routes, models, workflows, and product UI." ]
            docsSection "installation" "Installation" [
                docsCode "shell" "dotnet add package FSharp.ViewEngine.Docs" ]
            docsSection "browse" "Browse the toolkit" [
                docsCustom (
                    div {
                        _class "docs-catalog-grid"
                        catalogLink "/docs/components/layouts" "COMPONENTS" "Browse components" "Layouts, content, navigation, interactive examples, API reference primitives, and diagrams."
                        catalogLink "/docs/page-examples/documentation-site" "PAGE EXAMPLES" "Browse page examples" "Complete documentation, API-reference, and executable-specification compositions."
                    }) ] ]

    let layoutsPage =
        // docs-example:start document
        let documentPage =
            docsArticle "overview" "Acme Docs" "Build and ship with typed documentation." [
                docsSection "welcome" "Welcome" [ docsParagraph "Choose a guide to get started." ] ]

        let documentHtml =
            docsDocument exampleSite documentPage
            |> Render.toHtmlDocString
        // docs-example:end document

        // docs-example:start article
        let articlePage =
            docsArticle "guide" "Getting started" "Build your first integration." [
                docsSection "install" "Install" [
                    docsParagraph "Add the package to your application."
                    docsCode "shell" "dotnet add package Acme" ] ]
        // docs-example:end article

        // docs-example:start reference
        let referencePage =
            docsReference "customers" "Create a customer" "Customer API reference." [
                docsSection "endpoint" "Endpoint" [
                    docsCustom (docsApiEndpoint POST "/v1/customers" "Creates a customer.") ]
                docsSection "parameters" "Parameters" [
                    docsCustom (docsParameters [
                        docsParameter "email" "string" true "Customer email address." ]) ] ]
                (docsRail (docsCodeExample "Request" "curl" "curl -X POST /v1/customers"))
        // docs-example:end reference

        // docs-example:start canvas
        let canvasPage =
            docsCanvas "create-view" "Create a view" "Review the complete workflow." [
                docsSection "states" "States" [
                    docsCustom (docsStateTabs "view-states" "View states" [
                        { id = "ready"; label = "Ready"; content = productScreen "ready" }
                        { id = "validation"; label = "Validation"; content = productScreen "validation" } ]) ]
                docsSection "sequence" "Sequence" [ docsSequence (sequence ()) ] ]
        // docs-example:end canvas

        docsArticle layoutsRegistration.id layoutsRegistration.title "Shells and page layouts for guides, references, and wide product review surfaces." [
            componentExample "document" "docsDocument" "Render the complete branded document shell with navigation, assets, themes, metadata, and canonical URLs." (isolatedDocument "Complete documentation shell" "https://docs.example.test" documentHtml)
            componentExample "article" "docsArticle" "Use the article layout for guides and conceptual documentation with a readable content column and table of contents." (isolatedPage "Article layout" "https://docs.example.test/guide" articlePage)
            componentExample "reference" "docsReference" "Keep endpoint documentation beside request and response examples in a dedicated reference composition." (isolatedPage "Reference layout" "https://docs.example.test/customers" referencePage)
            componentExample "canvas" "docsCanvas" "Use a wide canvas for product frames, workflow states, and architecture diagrams; hide only the visual heading when the framed product already supplies one." (isolatedPage "Canvas layout" "https://docs.example.test/create-view" canvasPage) ]

    let contentPage =
        // docs-example:start sections-and-prose
        let prosePage =
            docsArticle "content-prose" "Content" "Typed prose blocks." [
                docsSection "install" "Install the package" [
                    docsParagraph "Compose readable documentation from typed content blocks."
                    docsBullets [ "Add the package"; "Configure assets"; "Render the document" ] ] ]
        // docs-example:end sections-and-prose

        // docs-example:start tables
        let tablePage =
            docsArticle "content-table" "Builder comparison" "Compact structured data." [
                docsSection "builders" "Builders" [
                    docsTable [ "Builder"; "Purpose" ] [
                        [ "docsArticle"; "Guides" ]
                        [ "docsCanvas"; "Product review" ] ] ] ]
        // docs-example:end tables

        // docs-example:start callouts
        let calloutPage =
            docsArticle "content-callout" "Security" "Important implementation guidance." [
                docsSection "boundary" "Trust boundary" [
                    docsCallout "Security" "Render trusted raw content only at an application-defined boundary." ] ]
        // docs-example:end callouts

        // docs-example:start code-and-custom
        let customPage =
            docsArticle "content-custom" "Output" "Code and product-owned HTML." [
                docsSection "output" "Output" [
                    docsCode "fsharp" "div { _class \"notice\"; \"Saved\" }"
                    docsCustom (div { _class "docs-notice-preview"; "Rendered output" }) ] ]
        // docs-example:end code-and-custom

        docsArticle contentRegistration.id contentRegistration.title "Typed blocks for readable prose, structured data, code, and custom composition." [
            componentExample "sections-and-prose" "Sections, prose, and lists" "Group content under semantic headings, then compose paragraphs and ordered or unordered lists." (isolatedPage "Sections, prose, and lists" "https://docs.example.test/content/prose" prosePage)
            componentExample "tables" "Tables" "Present compact metadata and comparisons with responsive horizontal overflow." (isolatedPage "Tables" "https://docs.example.test/content/tables" tablePage)
            componentExample "callouts" "Callouts" "Highlight a concise warning, note, or constraint without turning the page into a card grid." (isolatedPage "Callouts" "https://docs.example.test/content/callouts" calloutPage)
            componentExample "code-and-custom" "Code and custom HTML" "Use compact Prism-ready code blocks for source and docsCustom when a product needs its own typed HTML composition." (isolatedPage "Code and custom HTML" "https://docs.example.test/content/custom" customPage) ]

    let navigationPage =
        // docs-example:start navigation-tree
        let navigation = [
            docsNavPage "overview" "Overview" "/" "/"
            docsNavGroup "guides" "Guides" true [
                docsNavPage "install" "Installation" "/installation" "/installation" ] ]

        let navigationSite = { exampleSite with navigation = navigation }
        let navigationPreview =
            docsArticle "install" "Installation" "Install the package." []
            |> docsDocument navigationSite
            |> Render.toHtmlDocString
        // docs-example:end navigation-tree

        // docs-example:start page-pager
        let pagerPage =
            docsArticle "usage" "Usage" "Compose typed HTML." []
            |> docsWithPager (
                docsPager
                    (Some(docsPageLink "Installation" "/installation"))
                    (Some(docsPageLink "Extensions" "/extensions")))
        // docs-example:end page-pager

        // docs-example:start site-actions
        let siteWithActions =
            { exampleSite with
                defaultColorMode = DocsColorMode.System
                repository = Some(DocsRepository.github "https://github.com/example/project") }

        let actionsHtml =
            docsDocument siteWithActions pagerPage
            |> Render.toHtmlDocString
        // docs-example:end site-actions

        docsArticle navigationRegistration.id navigationRegistration.title "Discoverable navigation for the complete documentation journey." [
            componentExample "navigation-tree" "Navigation, breadcrumbs, and table of contents" "Use typed destinations for pages and destination-free groups; the shell derives side navigation and breadcrumbs while sections supply the local table of contents." (isolatedDocument "Navigation tree" "https://docs.example.test/installation" navigationPreview)
            componentExample "page-pager" "Previous and next" "Add an explicit learning path when the ideal reading order differs from the complete sidebar order." (isolatedPage "Previous and next" "https://docs.example.test/usage" pagerPage)
            componentExample "site-actions" "Theme and repository actions" "Configure System, Light, or Dark as the default and optionally expose a GitHub or custom repository destination." (isolatedDocument "Theme and repository actions" "https://docs.example.test/usage" actionsHtml) ]

    let interactivePage =
        // docs-example:start example
        let noticePreview =
            docsExample
                "notice-example"
                "Notice"
                "fsharp"
                "div { _class \"notice\"; \"Saved\" }"
                (div { _class "docs-notice-preview"; strong { "Saved" }; span { "The customer was updated." } })
        // docs-example:end example

        // docs-example:start state-tabs
        let readyView = productScreen "ready"
        let validationView = productScreen "validation"
        let states =
            docsStateTabs "component-workflow-states" "Workflow states" [
                { id = "ready"; label = "Ready"; content = readyView }
                { id = "validation"; label = "Validation"; content = validationView } ]
        // docs-example:end state-tabs

        // docs-example:start browser-frame
        let productUi = productView "browser-frame" "ready"
        let browserFramePreview =
            docsBrowserFrame
                "https://example.test/views/new"
                productUi
        // docs-example:end browser-frame

        docsArticle interactiveRegistration.id interactiveRegistration.title "Source-first examples and interactive product states for implementation and review." [
            componentExample "example" "docsExample" "Pair arbitrary rendered HTML with its source using independent, keyboard-accessible Code and Preview tabs." (previewSurface noticePreview)
            componentExample "state-tabs" "docsStateTabs" "Review empty, ready, validation, loading, or other meaningful states without duplicating the surrounding page." (previewSurface states)
            componentExample "browser-frame" "docsBrowserFrame" "Place product UI in a browser-like frame with an explicit canonical URL." (previewSurface browserFramePreview) ]

    let apiComponentsPage =
        // docs-example:start api-endpoint
        let endpoint =
            docsApiEndpoint
                POST
                "/v1/customers"
                "Creates a customer and returns its identifier."
        // docs-example:end api-endpoint

        // docs-example:start api-parameters
        let parameters =
            docsParameters [
                docsParameter "email" "string" true "Customer email address."
                docsParameter "metadata" "object" false "Application-defined values." ]
        // docs-example:end api-parameters

        // docs-example:start api-examples
        let requestSource = "curl -X POST https://api.example.test/v1/customers"
        let responseSource = "{ \"id\": \"cus_123\" }"
        let requestResponse =
            div {
                _class "docs-showcase-panels"
                docsCodeExample "Create customer" "curl" requestSource
                docsResponseExample "201" "json" responseSource
            }
        // docs-example:end api-examples

        docsArticle apiComponentsRegistration.id apiComponentsRegistration.title "Composable endpoint, parameter, request, and response primitives for product-owned APIs." [
            componentExample "api-endpoint" "Endpoint" "Present the HTTP method, path, and concise operation description." (previewSurface endpoint)
            componentExample "api-parameters" "Parameters" "Describe required and optional values with compact names, types, and explanations." (previewSurface parameters)
            componentExample "api-examples" "Request and response examples" "Keep realistic request source and response payloads visually paired." (previewSurface requestResponse) ]

    let diagramsPage =
        // docs-example:start mermaid
        let flowchart = """flowchart LR
    Developer[Developer] --> View[Typed view]
    View --> HTML[Encoded HTML]"""
        let mermaidPage =
            docsArticle "diagram" "Rendering flow" "Typed view rendering." [
                docsSection "flow" "Flow" [ docsDiagram flowchart ] ]
        // docs-example:end mermaid

        // docs-example:start c4
        let c4Source = """C4Context
    title Documentation context
    Person(dev, "Developer", "Authors documentation")
    System(docs, "Docs site", "Publishes documentation")
    Rel(dev, docs, "Uses")"""
        let c4Page =
            docsArticle "c4" "Documentation context" "System context." [
                docsSection "context" "Context" [ docsC4Diagram c4Source ] ]
        // docs-example:end c4

        // docs-example:start sequence-diagram
        let developer = SequenceDiagram.participant "Developer" "Developer"
        let engine = SequenceDiagram.participant "Engine" "View engine"
        let sequenceDiagram =
            SequenceDiagram.sequence [ developer; engine ] [
                SequenceDiagram.call developer engine "Render view"
                SequenceDiagram.reply engine developer "HTML" ]
        let sequencePage =
            docsArticle "sequence" "Render a view" "Rendering sequence." [
                docsSection "sequence" "Sequence" [ docsSequence sequenceDiagram ] ]
        // docs-example:end sequence-diagram

        docsArticle diagramsRegistration.id diagramsRegistration.title "Trusted Mermaid, C4, and validated sequence diagrams for architecture and workflow communication." [
            docsSection "live-diagram" "Live diagram" [
                docsParagraph "Diagram components own loading, rendering, theme changes, and an accessible unavailable state while product documentation supplies the trusted source."
                docsDiagram flowchart ]
            componentExample "mermaid" "Mermaid" "Render a trusted Mermaid source string in the standard responsive diagram surface." (isolatedPage "Mermaid diagram" "https://docs.example.test/diagrams/mermaid" mermaidPage)
            componentExample "c4" "C4" "Use Mermaid C4 syntax for a proportionate system context, container, component, dynamic, or deployment view." (isolatedPage "C4 diagram" "https://docs.example.test/diagrams/c4" c4Page)
            componentExample "sequence-diagram" "Sequence diagram" "Construct participants and calls with the validated sequence DSL before rendering Mermaid." (isolatedPage "Sequence diagram" "https://docs.example.test/diagrams/sequence" sequencePage) ]

    let documentationSitePage =
        // docs-example:start documentation-site-page
        let documentationPage =
            docsArticle "guide" "Getting started" "Build your first integration." [
                docsSection "install" "Install" [ docsCode "shell" "dotnet add package Acme" ] ]
            |> docsWithPager (docsPager (Some(docsPageLink "Overview" "/")) None)

        let documentationHtml =
            docsDocument exampleSite documentationPage
            |> Render.toHtmlDocString
        // docs-example:end documentation-site-page

        docsArticle documentationSiteRegistration.id documentationSiteRegistration.title "A complete guide composition using the shared shell, navigation, content, examples, and pager." [
            componentExample "documentation-site-page" "Guide with navigation" "This preview executes the same complete documentation page definition shown under Code." (isolatedDocument "Documentation site page example" "https://docs.example.test/guide" documentationHtml) ]

    let apiPageExample =
        // docs-example:start api-reference-page
        let endpoint = docsApiEndpoint POST "/v1/render" "Renders an HTML element."
        let parameters = docsParameters [ docsParameter "view" "string" true "Typed view source." ]
        let rail =
            div {
                docsCodeExample "Render a view" "fsharp" "Render.toString view"
                docsResponseExample "200" "html" "<main>Rendered view</main>" }
        let apiPage =
            docsReference "render" "Render a view" "Rendering reference." [
                docsSection "endpoint" "Endpoint" [ docsCustom endpoint ]
                docsSection "parameters" "Parameters" [ docsCustom parameters ] ]
                (docsRail rail)
        // docs-example:end api-reference-page

        docsArticle apiPageExampleRegistration.id apiPageExampleRegistration.title "A complete reference composition with endpoint documentation and a dedicated request-response rail." [
            docsSection "about" "About this page example" [
                docsParagraph "FSharp.ViewEngine does not expose an HTTP /v1/render endpoint. The fictional operation keeps the example focused on docsReference and the reusable API components."
                docsCustom (docsExample "docs-api-reference-page-example" "Render endpoint reference" "fsharp" (sourceFor "api-reference-page") (isolatedPage "API reference page example" "https://docs.example.test/render" apiPage)) ] ]

    let specificationPageExample =
        // docs-example:start executable-specification-page
        let readyView = productScreen "ready"
        let validationView = productScreen "validation"
        let states =
            docsStateTabs "page-example-render-states" "Render workflow states" [
                { id = "ready"; label = "Ready"; content = readyView }
                { id = "validation"; label = "Validation"; content = validationView } ]
        let specificationPage =
            docsCanvas "render-workflow" "Render a view" "Review the workflow." [
                docsSection "wireframe" "Wireframe" [ docsCustom states ]
                docsSection "sequence" "Sequence" [ docsSequence (sequence ()) ]
                docsSection "rules" "Rules" [ docsBullets [ "Encode text and attributes."; "Return deterministic HTML." ] ] ]
        // docs-example:end executable-specification-page

        docsArticle specificationPageExampleRegistration.id specificationPageExampleRegistration.title "A complete workflow review composition using a canvas, browser frames, state tabs, diagrams, and rules." [
            componentExample "executable-specification-page" "Render workflow" "The preview executes the same complete specification page definition shown under Code; consumers own the actual workflow, rules, and product UI." (isolatedPage "Executable specification page example" "https://docs.example.test/render-workflow" specificationPage) ]

    let private pages =
        [ overviewRegistration.path, overviewPage
          layoutsRegistration.path, layoutsPage
          contentRegistration.path, contentPage
          navigationRegistration.path, navigationPage
          interactiveRegistration.path, interactivePage
          apiComponentsRegistration.path, apiComponentsPage
          diagramsRegistration.path, diagramsPage
          documentationSiteRegistration.path, documentationSitePage
          apiPageExampleRegistration.path, apiPageExample
          specificationPageExampleRegistration.path, specificationPageExample ]
        |> Map.ofList

    let previewRoutes =
        previewDocuments
        |> Seq.map (fun pair -> pair.Key, pair.Value)
        |> Map.ofSeq

    let tryPage path = Map.tryFind path pages
