namespace Docs.Pages

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Primitives
open FSharp.ViewEngine.Components.Documentation
open type Html

module Showcase =
    let private registration (id:string) (path:string) (aliases:string list) (navLabel:string) (title:string) : DocPage =
        { id = id
          path = path
          aliases = aliases
          navLabel = navLabel
          category = "Documentation"
          title = title
          browserTitle = $"{title} · Components Documentation"
          nodes = [] }

    let overviewRegistration =
        registration "docs-overview" "/docs" [] "Overview" "Documentation"

    let layoutsRegistration =
        registration "docs-layouts" "/docs/components/layouts" [ "/docs/components"; "/docs-components" ] "Layouts" "Layouts"

    let contentRegistration =
        registration "docs-content" "/docs/components/content" [] "Content" "Content"

    let navigationRegistration =
        registration "docs-navigation" "/docs/components/navigation" [] "Navigation" "Navigation"

    let fixtureRegistration =
        registration "docs-fixture" "/docs/components/fixture" [] "Fixture" "Fixture"

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
          fixtureRegistration
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
        DocumentationSection.create id label [
            p { description }
            Example.previewFirst $"docs-{id}-example" label "fsharp" (sourceFor id) preview ]

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
                    Endpoint.create POST "/v1/customers"
                    |> Endpoint.withDescription "Creates a customer and returns its identifier."
                    |> Endpoint.render
                    div { _style "margin-top:1rem"; ApiReference.parameters [ ApiReference.parameter "email" "string" true "Customer email address." ] }
                }
                div {
                    ApiReference.codeExample "Request" "curl" "curl -X POST https://api.example.test/v1/customers"
                    ApiReference.responseExample "201" "json" "{ \"id\": \"cus_123\" }"
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
            [ Nav.group "guides" "Guides" true [
                Nav.page "overview" "Overview" "/" "/"
                Nav.page "guide" "Getting started" "/guide" "/guide" ] ]
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
            Browser.create (
                iframe {
                    _class "docs-isolated-document"
                    _title title
                    _data("docs-preview-src", previewPath)
                })
            |> Browser.withAddress canonicalUrl
            |> Browser.render)

    let private isolatedPage title canonicalUrl page =
        Document.create exampleSite page
        |> Document.render
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
        Browser.create (productView "workflow" state)
        |> Browser.withAddress "https://example.test/views/new"
        |> Browser.render

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
        DocumentationPage.create overviewRegistration.id overviewRegistration.title |> DocumentationPage.withDescription "Composable layouts and components for product documentation, API references, executable specifications, and component review." |> DocumentationPage.withSections [
            DocumentationSection.create "purpose" "Built with the package" [
                p { "This documentation site is built with FSharp.ViewEngine.Components.Documentation. The shell, navigation, themes, examples, API components, browser frames, and diagrams shown here use the public APIs in the unified Components package." }
                p { "The package owns documentation mechanics and composition while each product retains its content, information architecture, routes, models, workflows, and product UI." } ]
            DocumentationSection.create "installation" "Installation" [
                CodeBlock.create "shell" "dotnet add package FSharp.ViewEngine.Components" |> CodeBlock.render
                CodeBlock.create "fsharp" "open FSharp.ViewEngine.Components.Documentation" |> CodeBlock.render
                p { "This local candidate consolidates the former Docs package. The replacement release and deprecation are pending verification; existing pinned Docs versions are unchanged." } ]
            DocumentationSection.create "browse" "Browse the toolkit" [
                div {
                    _class "docs-catalog-grid"
                    catalogLink "/docs/components/layouts" "COMPONENTS" "Browse components" "Layouts, content, navigation, interactive examples, API reference primitives, and diagrams."
                    catalogLink "/docs/page-examples/documentation-site" "PAGE EXAMPLES" "Browse page examples" "Complete documentation, API-reference, and executable-specification compositions."
                } ] ]

    let layoutsPage =
        // docs-example:start document
        let site =
            DocsSite.create "Acme Docs" "overview"
            |> DocsSite.withAssets { DocsAssets.defaults with productStylesheets = [ "/css/output.css" ] }
        let documentPage =
            DocumentationPage.create "overview" "Acme Docs"
            |> DocumentationPage.withDescription "Build and ship with typed documentation."
            |> DocumentationPage.withSections [
                DocumentationSection.create "welcome" "Welcome" [
                    p { "Choose a guide to get started." } ] ]

        let documentHtml =
            Document.create site documentPage
            |> Document.render
            |> Render.toHtmlDocString
        // docs-example:end document

        // docs-example:start article
        let articlePage =
            DocumentationPage.create "guide" "Getting started" |> DocumentationPage.withDescription "Build your first integration." |> DocumentationPage.withSections [
                DocumentationSection.create "install" "Install" [
                    p { "Add the package to your application." }
                    CodeBlock.create "shell" "dotnet add package Acme" |> CodeBlock.render ] ]
        // docs-example:end article

        // docs-example:start reference
        let referencePage =
            DocumentationPage.create "customers" "Create a customer"
            |> DocumentationPage.withDescription "Customer API reference."
            |> DocumentationPage.withLayout Reference
            |> DocumentationPage.withRightRail (CustomRail (ApiReference.codeExample "Request" "curl" "curl -X POST /v1/customers"))
            |> DocumentationPage.withSections [
                DocumentationSection.create "endpoint" "Endpoint" [
                    Endpoint.create POST "/v1/customers"
                    |> Endpoint.withDescription "Creates a customer."
                    |> Endpoint.render ]
                DocumentationSection.create "parameters" "Parameters" [
                    ApiReference.parameters [
                        ApiReference.parameter "email" "string" true "Customer email address." ] ] ]
        // docs-example:end reference

        // docs-example:start canvas
        let canvasPage =
            DocumentationPage.create "create-view" "Create a view" |> DocumentationPage.withDescription "Review the complete workflow." |> DocumentationPage.withLayout Canvas |> DocumentationPage.withRightRail NoRail |> DocumentationPage.withSections [
                DocumentationSection.create "states" "States" [
                    [ Tab.create "ready" "Ready" (productScreen "ready")
                      Tab.create "validation" "Validation" (productScreen "validation") ]
                    |> Tabs.create "view-states" "View states"
                    |> Tabs.withVariant TabsVariant.Underlined
                    |> Tabs.render ]
                DocumentationSection.create "sequence" "Sequence" [
                    sequence () |> SequenceDiagram.render |> Mermaid.create |> Mermaid.render ] ]
        // docs-example:end canvas

        DocumentationPage.create layoutsRegistration.id layoutsRegistration.title |> DocumentationPage.withDescription "Shells and page layouts for guides, references, and wide product review surfaces." |> DocumentationPage.withSections [
            componentExample "document" "Document" "Render the complete branded document shell with navigation, assets, themes, metadata, and canonical URLs." (isolatedDocument "Complete documentation shell" "https://docs.example.test" documentHtml)
            componentExample "article" "Page article" "Use the default article layout for guides and conceptual documentation with a readable content column and table of contents." (isolatedPage "Article layout" "https://docs.example.test/guide" articlePage)
            componentExample "reference" "Page reference" "Use DocumentationPage.withLayout Reference to keep endpoint documentation beside request and response examples." (isolatedPage "Reference layout" "https://docs.example.test/customers" referencePage)
            componentExample "canvas" "Page canvas" "Use DocumentationPage.withLayout Canvas for product frames, workflow states, and architecture diagrams; DocumentationPage.withHiddenHeading retains a semantic heading when the framed product already supplies one." (isolatedPage "Canvas layout" "https://docs.example.test/create-view" canvasPage) ]

    let contentPage =
        // docs-example:start sections-and-prose
        let prosePage =
            DocumentationPage.create "content-prose" "Content"
            |> DocumentationPage.withDescription "Typed prose blocks."
            |> DocumentationPage.withSections [
                DocumentationSection.create "install" "Install the package" [
                    p { "Compose readable documentation from typed HTML." }
                    ol {
                        li { "Add the package" }
                        li { "Configure assets" }
                        li { "Render the document" }
                    } ] ]
        // docs-example:end sections-and-prose

        // docs-example:start tables
        let tablePage =
            DocumentationPage.create "content-table" "Builder comparison" |> DocumentationPage.withDescription "Compact structured data." |> DocumentationPage.withSections [
                DocumentationSection.create "builders" "Builders" [
                    div {
                        _class "spec-table-wrap"
                        table {
                            _class "spec-table"
                            thead { tr { th { "Builder" }; th { "Purpose" } } }
                            tbody {
                                tr { td { "DocumentationPage.create" }; td { "Guides" } }
                                tr { td { "DocumentationPage.withLayout Canvas" }; td { "Product review" } }
                            }
                        }
                    } ] ]
        // docs-example:end tables

        // docs-example:start callouts
        let calloutPage =
            DocumentationPage.create "content-callout" "Security" |> DocumentationPage.withDescription "Important implementation guidance." |> DocumentationPage.withSections [
                DocumentationSection.create "boundary" "Trust boundary" [
                    Callout.create "Security" [ p { "Render trusted raw content only at an application-defined boundary." } ]
                    |> Callout.render ] ]
        // docs-example:end callouts

        // docs-example:start code-and-custom
        let customPage =
            DocumentationPage.create "content-custom" "Output" |> DocumentationPage.withDescription "Code and product-owned HTML." |> DocumentationPage.withSections [
                DocumentationSection.create "output" "Output" [
                    CodeBlock.create "fsharp" "div { _class \"notice\"; \"Saved\" }" |> CodeBlock.render
                    div { _class "docs-notice-preview"; "Rendered output" } ] ]
        // docs-example:end code-and-custom

        DocumentationPage.create contentRegistration.id contentRegistration.title |> DocumentationPage.withDescription "Typed blocks for readable prose, structured data, code, and custom composition." |> DocumentationPage.withSections [
            componentExample "sections-and-prose" "Sections, prose, and lists" "Group content under semantic headings, then compose paragraphs and ordered or unordered lists." (isolatedPage "Sections, prose, and lists" "https://docs.example.test/content/prose" prosePage)
            componentExample "tables" "Tables" "Present compact metadata and comparisons with responsive horizontal overflow." (isolatedPage "Tables" "https://docs.example.test/content/tables" tablePage)
            componentExample "callouts" "Callouts" "Highlight a concise warning, note, or constraint without turning the page into a card grid." (isolatedPage "Callouts" "https://docs.example.test/content/callouts" calloutPage)
            componentExample "code-and-custom" "Code and custom HTML" "Use CodeBlock for Prism-ready source and compose product-owned typed HTML directly when a page needs custom content." (isolatedPage "Code and custom HTML" "https://docs.example.test/content/custom" customPage) ]

    let navigationPage =
        // docs-example:start navigation-tree
        let navigation = [
            Nav.page "overview" "Overview" "/" "/"
            Nav.group "guides" "Guides" true [
                Nav.page "install" "Installation" "/installation" "/installation" ] ]

        let navigationSite = { exampleSite with navigation = navigation }
        let navigationPreview =
            DocumentationPage.create "install" "Installation"
            |> DocumentationPage.withDescription "Install the package."
            |> DocumentationPage.withSections []
            |> fun page -> Document.create navigationSite page
            |> Document.render
            |> Render.toHtmlDocString
        // docs-example:end navigation-tree

        // docs-example:start page-pager
        let pagerPage =
            DocumentationPage.create "usage" "Usage" |> DocumentationPage.withDescription "Compose typed HTML." |> DocumentationPage.withSections []
            |> DocumentationPage.withPager (
                DocsPager.create
                    (Some(DocsPageLink.create "Installation" "/installation"))
                    (Some(DocsPageLink.create "Extensions" "/extensions")))
        // docs-example:end page-pager

        // docs-example:start site-actions
        let siteWithActions =
            { exampleSite with
                defaultColorMode = DocsColorMode.System
                repository = Some(DocsRepository.github "https://github.com/example/project") }

        let actionsHtml =
            Document.create siteWithActions pagerPage
            |> Document.render
            |> Render.toHtmlDocString
        // docs-example:end site-actions

        DocumentationPage.create navigationRegistration.id navigationRegistration.title |> DocumentationPage.withDescription "Discoverable navigation for the complete documentation journey." |> DocumentationPage.withSections [
            componentExample "navigation-tree" "Navigation, breadcrumbs, and table of contents" "Use typed destinations for pages and destination-free groups; the shell derives side navigation and breadcrumbs while sections supply the local table of contents." (isolatedDocument "Navigation tree" "https://docs.example.test/installation" navigationPreview)
            componentExample "page-pager" "Previous and next" "Add an explicit learning path when the ideal reading order differs from the complete sidebar order." (isolatedPage "Previous and next" "https://docs.example.test/usage" pagerPage)
            componentExample "site-actions" "Theme and repository actions" "Configure System, Light, or Dark as the default and optionally expose a GitHub or custom repository destination." (isolatedDocument "Theme and repository actions" "https://docs.example.test/usage" actionsHtml) ]

    let private fixtureHref state = $"/docs/components/fixture?fixtureState={state}"

    let fixturePageFor fixtureState =
        let fixtureState = if fixtureState = "validation" then "validation" else "ready"
        // docs-example:start state-tabs
        let readyView = productScreen "ready"
        let validationView = productScreen "validation"
        let states =
            [ Tab.create "ready" "Ready" readyView
              Tab.create "validation" "Validation" validationView ]
            |> Tabs.create "component-workflow-states" "Workflow states"
            |> Tabs.withVariant TabsVariant.Underlined
            |> Tabs.render
        // docs-example:end state-tabs

        // docs-example:start browser-frame
        let productUi = productView "browser-frame" "ready"
        let browserFramePreview =
            Browser.create productUi
            |> Browser.withAddress "https://example.test/views/new"
            |> Browser.render
        // docs-example:end browser-frame

        // docs-example:start app-mode
        let appModeStates =
            [ FixtureState.create "Ready" (fixtureHref "ready")
              FixtureState.create "Validation" (fixtureHref "validation") ]
            |> List.mapi (fun index state ->
                if (fixtureState = "ready" && index = 0) || (fixtureState = "validation" && index = 1) then FixtureState.current state
                else state)

        let appModeFixture =
            Browser.create (productView "app-mode-browser" fixtureState)
            |> Browser.withAddress "https://example.test/views/new"
            |> Browser.withAppMode "view-studio-browser" "Create a view"
            |> Browser.render
            |> Fixture.create "view-studio-browser"
            |> Fixture.withStates appModeStates
            |> Fixture.render
        // docs-example:end app-mode

        // docs-example:start app-mode-phone
        let appModePhoneFixture =
            Phone.create (
                div {
                    _class "docs-app-mode-phone-screen"
                    header { _class "docs-app-mode-phone-header"; strong { "View Studio" }; span { "FS" } }
                    main {
                        _class "docs-app-mode-phone-content"
                        small { "NEW VIEW" }
                        h3 { "Render your first component" }
                        p { "Give the view a name before choosing its output." }
                        label { _for "app-mode-phone-view-name"; "View name" }
                        input { _id "app-mode-phone-view-name"; _value "accountSummary" }
                        button { _type "button"; _class "docs-product-primary"; "Create view" }
                    }
                })
            |> Phone.withAppMode "view-studio-phone" "Create a view on phone"
            |> Phone.render
            |> Fixture.create "view-studio-phone"
            |> Fixture.render
        // docs-example:end app-mode-phone

        // docs-example:start fixture-workflow
        let fixtureHref state = $"/docs/components/fixture?fixtureState={state}"
        let shippingFixture =
            Browser.create (
                div {
                    _class "grid min-h-64 content-center gap-3 bg-[var(--fve-surface-subtle)] p-8 text-center"
                    strong { _class "text-lg"; "Shipping address" }
                    p { _class "text-sm text-[var(--fve-muted-text)]"; "Enter a delivery address to continue." }
                })
            |> Browser.withAddress "https://shop.example.test/checkout/shipping"
            |> Browser.withAppMode "checkout-shipping" "Shipping address"
            |> Browser.render
            |> Fixture.create "checkout-shipping"
            |> Fixture.withPrevious (FixtureLink.create "Cart" "https://shop.example.test/checkout/cart")
            |> Fixture.withNext (FixtureLink.create "Payment" "https://shop.example.test/checkout/payment")
            |> Fixture.withStates [
                FixtureState.create "Ready" (fixtureHref "ready") |> FixtureState.current
                FixtureState.create "Address error" (fixtureHref "validation") ]
            |> Fixture.render
        // docs-example:end fixture-workflow

        DocumentationPage.create fixtureRegistration.id fixtureRegistration.title |> DocumentationPage.withDescription "Compose Browser or Phone review fixtures with source, preview, and copyable code without giving Documentation ownership of product routes or state." |> DocumentationPage.withSections [
            componentExample "state-tabs" "Review states" "Use state tabs for static comparison, or Fixture review states when the viewer needs a navigable alternate product state." (previewSurface states)
            componentExample "browser-frame" "Browser frame" "Place product UI in a browser-like frame with an explicit canonical URL before optionally enabling App mode." (previewSurface browserFramePreview)
            componentExample "app-mode" "Browser fixture" "Expand a named Browser fixture to a focused executable preview. Fixture supplies independent review states while the product owns their meaning." (previewSurface appModeFixture)
            componentExample "app-mode-phone" "Phone fixture" "Use the same viewer contract for a Phone fixture; the viewer controls the surface while the product owns screen content." (previewSurface appModePhoneFixture)
            componentExample "fixture-workflow" "Workflow fixture" "Fixture supplies authored previous and next destinations independently from review states. Its source, preview, and copy action stay together." (previewSurface shippingFixture) ]

    let apiComponentsPage =
        // docs-example:start api-endpoint
        let endpoint =
            Endpoint.create POST "/v1/customers"
            |> Endpoint.withDescription "Creates a customer and returns its identifier."
            |> Endpoint.render
        // docs-example:end api-endpoint

        // docs-example:start api-parameters
        let parameters =
            ApiReference.parameters [
                ApiReference.parameter "email" "string" true "Customer email address."
                ApiReference.parameter "metadata" "object" false "Application-defined values." ]
        // docs-example:end api-parameters

        // docs-example:start api-examples
        let requestSource = "curl -X POST https://api.example.test/v1/customers"
        let responseSource = "{ \"id\": \"cus_123\" }"
        let requestResponse =
            div {
                _class "docs-showcase-panels"
                ApiReference.codeExample "Create customer" "curl" requestSource
                ApiReference.responseExample "201" "json" responseSource
            }
        // docs-example:end api-examples

        DocumentationPage.create apiComponentsRegistration.id apiComponentsRegistration.title |> DocumentationPage.withDescription "Composable endpoint, parameter, request, and response primitives for product-owned APIs." |> DocumentationPage.withSections [
            componentExample "api-endpoint" "Endpoint" "Present the HTTP method, path, and concise operation description." (previewSurface endpoint)
            componentExample "api-parameters" "Parameters" "Describe required and optional values with compact names, types, and explanations." (previewSurface parameters)
            componentExample "api-examples" "Request and response examples" "Keep realistic request source and response payloads visually paired." (previewSurface requestResponse) ]

    let diagramsPage =
        // docs-example:start mermaid
        let flowchart = """flowchart LR
    Developer[Developer] --> View[Typed view]
    View --> HTML[Encoded HTML]"""
        let mermaidPage =
            DocumentationPage.create "diagram" "Rendering flow" |> DocumentationPage.withDescription "Typed view rendering." |> DocumentationPage.withSections [
                DocumentationSection.create "flow" "Flow" [ Mermaid.create flowchart |> Mermaid.render ] ]
        // docs-example:end mermaid

        // docs-example:start c4
        let c4Source = """C4Context
    title Documentation context
    Person(dev, "Developer", "Authors documentation")
    System(docs, "Docs site", "Publishes documentation")
    Rel(dev, docs, "Uses")"""
        let c4Page =
            DocumentationPage.create "c4" "Documentation context" |> DocumentationPage.withDescription "System context." |> DocumentationPage.withSections [
                DocumentationSection.create "context" "Context" [ Mermaid.create c4Source |> Mermaid.withC4 |> Mermaid.render ] ]
        // docs-example:end c4

        // docs-example:start sequence-diagram
        let developer = SequenceDiagram.participant "Developer" "Developer"
        let engine = SequenceDiagram.participant "Engine" "View engine"
        let sequenceDiagram =
            SequenceDiagram.sequence [ developer; engine ] [
                SequenceDiagram.call developer engine "Render view"
                SequenceDiagram.reply engine developer "HTML" ]
        let sequencePage =
            DocumentationPage.create "sequence" "Render a view" |> DocumentationPage.withDescription "Rendering sequence." |> DocumentationPage.withSections [
                DocumentationSection.create "sequence" "Sequence" [ sequenceDiagram |> SequenceDiagram.render |> Mermaid.create |> Mermaid.render ] ]
        // docs-example:end sequence-diagram

        DocumentationPage.create diagramsRegistration.id diagramsRegistration.title |> DocumentationPage.withDescription "Trusted Mermaid, C4, and validated sequence diagrams for architecture and workflow communication." |> DocumentationPage.withSections [
            DocumentationSection.create "live-diagram" "Live diagram" [
                p { "Diagram components own loading, rendering, theme changes, and an accessible unavailable state while product documentation supplies the trusted source." }
                Mermaid.create flowchart |> Mermaid.render ]
            componentExample "mermaid" "Mermaid" "Render a trusted Mermaid source string in the standard responsive diagram surface." (isolatedPage "Mermaid diagram" "https://docs.example.test/diagrams/mermaid" mermaidPage)
            componentExample "c4" "C4" "Use Mermaid C4 syntax for a proportionate system context, container, component, dynamic, or deployment view." (isolatedPage "C4 diagram" "https://docs.example.test/diagrams/c4" c4Page)
            componentExample "sequence-diagram" "Sequence diagram" "Construct participants and calls with the validated sequence DSL before rendering Mermaid." (isolatedPage "Sequence diagram" "https://docs.example.test/diagrams/sequence" sequencePage) ]

    let documentationSitePage =
        // docs-example:start documentation-site-page
        let documentationPage =
            DocumentationPage.create "guide" "Getting started" |> DocumentationPage.withDescription "Build your first integration." |> DocumentationPage.withSections [
                DocumentationSection.create "install" "Install" [ CodeBlock.create "shell" "dotnet add package Acme" |> CodeBlock.render ] ]
            |> DocumentationPage.withPager (DocsPager.create (Some(DocsPageLink.create "Overview" "/")) None)

        let documentationHtml =
            Document.create exampleSite documentationPage
            |> Document.render
            |> Render.toHtmlDocString
        // docs-example:end documentation-site-page

        DocumentationPage.create documentationSiteRegistration.id documentationSiteRegistration.title |> DocumentationPage.withDescription "A complete guide composition using the shared shell, navigation, content, examples, and pager." |> DocumentationPage.withSections [
            componentExample "documentation-site-page" "Guide with navigation" "This preview executes the same complete documentation page definition shown under Code." (isolatedDocument "Documentation site page example" "https://docs.example.test/guide" documentationHtml) ]

    let apiPageExample =
        // docs-example:start api-reference-page
        let endpoint =
            Endpoint.create POST "/v1/render"
            |> Endpoint.withDescription "Renders an HTML element."
            |> Endpoint.render
        let parameters = ApiReference.parameters [ ApiReference.parameter "view" "string" true "Typed view source." ]
        let rail =
            div {
                ApiReference.codeExample "Render a view" "fsharp" "Render.toString view"
                ApiReference.responseExample "200" "html" "<main>Rendered view</main>" }
        let apiPage =
            DocumentationPage.create "render" "Render a view"
            |> DocumentationPage.withDescription "Rendering reference."
            |> DocumentationPage.withLayout Reference
            |> DocumentationPage.withRightRail (CustomRail rail)
            |> DocumentationPage.withSections [
                DocumentationSection.create "endpoint" "Endpoint" [ endpoint ]
                DocumentationSection.create "parameters" "Parameters" [ parameters ] ]
        // docs-example:end api-reference-page

        DocumentationPage.create apiPageExampleRegistration.id apiPageExampleRegistration.title |> DocumentationPage.withDescription "A complete reference composition with endpoint documentation and a dedicated request-response rail." |> DocumentationPage.withSections [
            DocumentationSection.create "about" "About this page example" [
                p { "FSharp.ViewEngine does not expose an HTTP /v1/render endpoint. The fictional operation keeps the example focused on DocumentationPage.withLayout Reference and the reusable API components." }
                Example.previewFirst "docs-api-reference-page-example" "Render endpoint reference" "fsharp" (sourceFor "api-reference-page") (isolatedPage "API reference page example" "https://docs.example.test/render" apiPage) ] ]

    let specificationPageExample =
        // docs-example:start executable-specification-page
        let readyView = productScreen "ready"
        let validationView = productScreen "validation"
        let states =
            [ Tab.create "ready" "Ready" readyView
              Tab.create "validation" "Validation" validationView ]
            |> Tabs.create "page-example-render-states" "Render workflow states"
            |> Tabs.withVariant TabsVariant.Underlined
            |> Tabs.render
        let specificationPage =
            DocumentationPage.create "render-workflow" "Render a view" |> DocumentationPage.withDescription "Review the workflow." |> DocumentationPage.withLayout Canvas |> DocumentationPage.withRightRail NoRail |> DocumentationPage.withSections [
                DocumentationSection.create "wireframe" "Wireframe" [ states ]
                DocumentationSection.create "sequence" "Sequence" [ sequence () |> SequenceDiagram.render |> Mermaid.create |> Mermaid.render ]
                DocumentationSection.create "rules" "Rules" [
                    ul {
                        li { "Encode text and attributes." }
                        li { "Return deterministic HTML." }
                    } ] ]
        // docs-example:end executable-specification-page

        DocumentationPage.create specificationPageExampleRegistration.id specificationPageExampleRegistration.title |> DocumentationPage.withDescription "A complete workflow review composition using a canvas, browser frames, tabs, diagrams, and rules." |> DocumentationPage.withSections [
            componentExample "executable-specification-page" "Render workflow" "The preview executes the same complete specification page definition shown under Code; consumers own the actual workflow, rules, and product UI." (isolatedPage "Executable specification page example" "https://docs.example.test/render-workflow" specificationPage) ]

    let private pages =
        [ overviewRegistration.path, overviewPage
          layoutsRegistration.path, layoutsPage
          contentRegistration.path, contentPage
          navigationRegistration.path, navigationPage
          fixtureRegistration.path, fixturePageFor "ready"
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
