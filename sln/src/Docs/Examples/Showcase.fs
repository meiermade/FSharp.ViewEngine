namespace Docs.Pages

open System
open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

module Showcase =
    let private publicOrigin = "https://fve.meiermade.com"
    let private publicUrl path = publicOrigin + path

    let private registration
        (id: string)
        (path: string)
        (aliases: string list)
        (navLabel: string)
        (title: string)
        : DocPage =
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
        registration
            "docs-layouts"
            "/docs/components/layouts"
            [ "/docs/components"; "/docs-components" ]
            "Layouts"
            "Layouts"

    let contentRegistration =
        registration "docs-content" "/docs/components/content" [] "Content" "Content"

    let navigationRegistration =
        registration "docs-navigation" "/docs/components/navigation" [] "Navigation" "Navigation"

    let fixtureRegistration =
        registration "docs-fixture" "/docs/components/fixture" [] "Fixture" "Fixture"

    let apiComponentsRegistration =
        registration
            "docs-api-components"
            "/docs/components/api-reference"
            []
            "API reference"
            "API reference components"

    let diagramsRegistration =
        registration "docs-diagrams" "/docs/components/diagrams" [] "Diagrams" "Diagrams"

    let documentationSiteRegistration =
        registration
            "docs-page-documentation-site"
            "/docs/page-examples/documentation-site"
            []
            "Documentation site"
            "Documentation site"

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

    let private previewSurface (content: HtmlElement) =
        div {
            _data ("example-surface", "true")
            _class "min-w-0"
            content
        }

    let private sourceText =
        lazy (SourceRegion.readEmbedded typeof<DocPage>.Assembly "Docs.Pages.Showcase.fs")

    let sourceFor id =
        SourceRegion.extract id sourceText.Value

    let private componentExample (id: string) (label: string) (description: string) (preview: HtmlElement) =
        DocumentationSection.create
            id
            label
            [ p {
                  _class "m-0 text-base leading-relaxed text-[var(--fve-muted-text)]"
                  description
              }
              fveExample {
                  _name $"docs-{id}-example"
                  _label label
                  _variant ExampleVariant.Gallery
                  _language "fsharp"
                  fveExampleCode { sourceFor id }
                  preview
              } ]

    let private componentPage (registration: DocPage) description _owner installation (id, label, preview: HtmlElement) variants =
        let source = sourceFor id

        let section id title content =
            DocumentationSection.create
                id
                title
                (h2 {
                    _class "text-xl font-semibold text-[var(--fve-text)]"
                    title
                 }
                 :: content)

        DocumentationPage.create registration.id registration.title
        |> DocumentationPage.withDescription description
        |> DocumentationPage.withLayout Gallery
        |> DocumentationPage.withRightRail TableOfContents
        |> DocumentationPage.withLead
            [ div {
                  _id id

                  fveExample {
                      _name $"docs-{id}-example"
                      _label label
                      _variant ExampleVariant.Lead
                      _language "fsharp"
                      fveExampleCode { source }
                      preview
                  }
              } ]
        |> DocumentationPage.withSections
            [ section
                  "installation"
                  "Installation"
                  [ p {
                        _class "m-0 text-base leading-relaxed text-[var(--fve-muted-text)]"
                        "Copy the component and its dependencies into your consumer-owned Components project."
                    }
                    fveCodeBlock {
                        _language "shell"
                        $"dotnet fve add {installation} --config src/Acme.Components/fve.json"
                    }
                    p {
                        "See "

                        a {
                            _href "/components/installation"
                            "Installation"
                        }

                        " for project initialization and Tailwind source detection."
                    } ]
              section
                  "usage"
                  "Usage"
                  [ fveCodeBlock {
                        _language "fsharp"
                        source
                    } ]
              yield! variants ]

    let private buildingBlockLinks label (items: (string * string) list) =
        div {
            _class "flex flex-wrap items-baseline gap-x-4 gap-y-2"

            h2 {
                _class "text-base font-normal"
                "Built with"
            }

            nav {
                _ariaLabel label
                _class "flex flex-wrap gap-x-4 gap-y-2"

                for path, itemLabel in items do
                    a {
                        _href path
                        text itemLabel
                    }
            }
        }

    let private catalogLink (href: string) (eyebrow: string) (title: string) (description: string) =
        a {
            _href href
            _class "docs-catalog-card"

            span {
                _class "docs-catalog-eyebrow"
                eyebrow
            }

            strong { title }

            span {
                _class "docs-catalog-description"
                description
            }

            span {
                _class "docs-catalog-action"
                "Browse "
                raw "&rarr;"
            }
        }

    let private miniatureArticle () =
        previewSurface (
            div {
                _class "docs-mini-shell"

                aside {
                    div {
                        _class "docs-mini-brand"
                        "Acme Docs"
                    }

                    div {
                        _class "docs-mini-nav-active"
                        "Getting started"
                    }

                    div { "Installation" }
                    div { "Configuration" }
                }

                div {
                    _class "docs-mini-main"

                    div {
                        _class "docs-mini-breadcrumb"
                        "Guides / Getting started"
                    }

                    h3 { "Build your first integration" }
                    p { "Compose a focused guide with navigation and an on-this-page rail." }

                    div {
                        _class "docs-mini-code"
                        "dotnet add package Acme"
                    }
                }

                nav {
                    _ariaLabel "Example table of contents"
                    small { "ON THIS PAGE" }
                    div { "Install" }
                    div { "Configure" }
                }
            }
        )

    let private miniatureReference () =
        previewSurface (
            div {
                _class "docs-showcase-reference"

                div {
                    h3 { "Create a customer" }

                    ApiEndpoint.create POST "/v1/customers"
                    |> ApiEndpoint.withDescription "Creates a customer and returns its identifier."
                    |> ApiEndpoint.render

                    div {
                        _style "margin-top:1rem"

                        ApiReference.parameters
                            [ ApiReference.parameter "email" "string" true "Customer email address." ]
                    }
                }

                div {
                    ApiReference.codeExample "Request" "curl" "curl -X POST $API_ORIGIN/v1/customers"
                    ApiReference.responseExample "201" "json" "{ \"id\": \"cus_123\" }"
                }
            }
        )

    let private exampleSite: DocsSite<string> =
        { name = "Acme Docs"
          baseUrl = Some publicOrigin
          description = Some "Example documentation"
          repository = None
          brandMark = span { "AC" }
          homeId = "overview"
          navigation =
            [ Nav.group
                  "guides"
                  "Guides"
                  true
                  [ Nav.page "overview" "Overview" "/" "/"
                    Nav.page "guide" "Getting started" "/getting-started/first-view" "/getting-started/first-view" ] ]
          storageKey = "fsharp-view-engine-docs-example"
          defaultColorMode = DocsColorMode.System
          theme = DocsTheme.sky
          assets =
            { DocsAssets.defaults with
                productStylesheets = [ "/css/output.css" ] }
          search = [] }

    let private previewDocuments =
        System.Collections.Generic.Dictionary<string, string>()

    let private previewPages =
        System.Collections.Generic.Dictionary<string, DocsSite<string> * DocumentationPageConfig>()

    let private previewToken (title: string) =
        title.ToLowerInvariant()
        |> Seq.map (fun character ->
            if System.Char.IsLetterOrDigit character then
                character
            else
                '-')
        |> Seq.toArray
        |> System.String

    let private registerPreviewDocument previewPath (html: string) =
        if
            not (
                html
                    .TrimStart()
                    .StartsWith("<!DOCTYPE html>", System.StringComparison.OrdinalIgnoreCase)
            )
        then
            invalidArg
                (nameof html)
                "Isolated previews must be complete HTML documents. Render component fragments directly in the host preview."

        if not (previewDocuments.ContainsKey previewPath) then
            previewDocuments.Add(previewPath, html)

    let private previewContent title previewPath fullscreen =
        iframe {
            _class (
                "docs-isolated-document block w-full border-0 bg-[var(--fve-background)]"
                + (if fullscreen then " h-dvh" else " min-h-128")
            )

            _title title
            _src previewPath
            _data ("docs-preview-src", previewPath)
        }

    let private browserConfig title canonicalUrl previewPath =
        fveBrowser {
            _address canonicalUrl
            previewContent title previewPath false
        }

    let private browserFrame title canonicalUrl previewPath =
        browserConfig title canonicalUrl previewPath

    let private isolatedDocumentFrame (title: string) (canonicalUrl: string) (html: string) =
        let token = previewToken title
        let previewPath = $"/docs/previews/{token}"
        registerPreviewDocument previewPath html
        browserFrame title canonicalUrl previewPath |> previewSurface

    let private reviewStateTabs (label: string) (current: string) (states: (string * string * string) list) =
        nav {
            _ariaLabel label
            _attr ("data-page-example-state-tabs", "true")
            _class "mb-5 border-b border-[var(--fve-border)]"

            ul {
                _class "-mb-px flex list-none gap-5 overflow-x-auto p-0"

                for state, stateLabel, href in states do
                    li {
                        a {
                            _href href

                            if state = current then
                                _ariaCurrent "page"

                            _class (
                                if state = current then
                                    "inline-flex min-h-11 items-center whitespace-nowrap border-b-2 border-[var(--fve-brand-solid)] font-semibold text-[var(--fve-brand-text)]"
                                else
                                    "inline-flex min-h-11 items-center whitespace-nowrap border-b-2 border-transparent font-medium text-[var(--fve-muted-text)] hover:border-[var(--fve-border)] hover:text-[var(--fve-text)]"
                            )

                            stateLabel
                        }
                    }
            }
        }

    let private statefulDocumentFixture
        token
        title
        launchHref
        canonicalUrl
        previewPath
        current
        (states: (string * string * string) list)
        =
        browserConfig title canonicalUrl previewPath
        |> Fixture.create token title launchHref
        |> Fixture.withFullscreenContent (previewContent title previewPath true)
        |> Fixture.withStates
            [ for state, stateLabelText, href in states ->
                  FixtureState.create stateLabelText href
                  |> fun item -> if state = current then FixtureState.current item else item ]

    let private statefulDocumentFrame fixture stateLabel current (states: (string * string * string) list) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _attr ("data-page-example-preview", "true")
            reviewStateTabs stateLabel current states
            fixture |> Fixture.render
        }

    let private isolatedDocument = isolatedDocumentFrame

    let private renderDocument site page =
        Document.create site page |> Document.render |> Render.toHtmlDocString

    let private registerPreviewPage path site page =
        registerPreviewDocument path (renderDocument site page)
        previewPages.TryAdd(path, (site, page)) |> ignore

    let tryPreviewPage path =
        match previewPages.TryGetValue path with
        | true, page -> Some page
        | _ -> None

    let private renderIsolatedPage isolatedDocument title canonicalUrl page =
        renderDocument exampleSite page |> isolatedDocument title canonicalUrl

    let private isolatedPage title canonicalUrl page =
        registerPreviewPage ($"/docs/previews/{previewToken title}") exampleSite page
        renderIsolatedPage isolatedDocument title canonicalUrl page

    let private productView (instanceId: string) (state: string) =
        let hasValidation = state = "validation"
        let suffix = instanceId + (if hasValidation then "-validation" else "-ready")

        div {
            _class "docs-product-screen grid min-h-96 content-start bg-[var(--fve-background)] text-[var(--fve-text)]"

            header {
                _class
                    "flex flex-wrap items-center justify-between gap-2 border-b border-[var(--fve-border)] px-4 py-3 text-sm"

                strong { "View Studio" }
                span { "Static wireframe" }
            }

            div {
                _class "grid gap-4 p-6"

                h3 {
                    _class "m-0 text-xl font-semibold"
                    "Create a view"
                }

                p {
                    _class "m-0 text-sm text-[var(--fve-muted-text)]"
                    "Give this view a name so your team can find it later."
                }

                let field =
                    fveField {
                        fveLabel {
                            _for $"view-name-{suffix}"
                            "View name"
                        }

                        fveInput {
                            _id $"view-name-{suffix}"
                            _name "viewName"
                            _value (if hasValidation then "" else "accountSummary")
                            _disabled true
                            _ariaInvalid hasValidation

                            if hasValidation then
                                _ariaDescribedby $"view-name-{suffix}-validation"
                        }

                        if hasValidation then
                            fveFieldError {
                                _id $"view-name-{suffix}-validation"
                                "Enter a view name."
                            }
                    }

                field

                div {
                    _class "flex flex-wrap items-center gap-2"

                    fveButton {
                        _color Color.Primary
                        _variant ButtonVariant.Solid
                        _disabled true
                        "Create view"
                    }

                    fveButton {
                        _disabled true
                        "Cancel"
                    }
                }

                p {
                    _class "m-0 text-xs text-[var(--fve-muted-text)]"
                    "Controls are disabled. Ready and Validation compare presentation states, not a working form."
                }
            }
        }

    let private productScreen state =
        fveBrowser {
            _address (publicUrl $"{fixtureRegistration.path}?fixtureState={state}")
            productView "workflow" state
        }

    let private sequence () =
        let developer = SequenceDiagram.participant "Developer" "Developer"
        let engine = SequenceDiagram.participant "Engine" "View engine"
        let output = SequenceDiagram.participant "Output" "HTML output"

        SequenceDiagram.sequence
            [ developer; engine; output ]
            [ SequenceDiagram.call developer engine "Build HtmlElement"
              SequenceDiagram.call developer engine "Render.toString element"
              SequenceDiagram.call engine output "Encode and serialize"
              SequenceDiagram.reply output developer "HTML string" ]

    let overviewPage =
        DocumentationPage.create overviewRegistration.id overviewRegistration.title
        |> DocumentationPage.withDescription
            "Composable layouts and components for product documentation, API references, executable specifications, and component review."
        |> DocumentationPage.withSections
            [ DocumentationSection.create
                  "purpose"
                  "Built with canonical source"
                  [ p {
                        _class "m-0 text-base leading-relaxed text-[var(--fve-muted-text)] [overflow-wrap:anywhere]"
                        "This documentation site compiles the same FSharp.ViewEngine.Components.Templates source distributed by fve. The shell, navigation, themes, examples, API components, browser frames, and diagrams shown here use those public APIs."
                    }
                    p {
                        "Documentation components own mechanics and composition while each product retains its content, information architecture, routes, models, workflows, and product UI."
                    } ]
              DocumentationSection.create
                  "installation"
                  "Installation"
                  [ fveCodeBlock {
                        _language "shell"
                        "dotnet fve add documentation --config src/Acme.Components/fve.json"
                    }
                    fveCodeBlock {
                        _language "fsharp"
                        "open Acme.Components.Documentation"
                    } ]
              DocumentationSection.create
                  "browse"
                  "Browse the toolkit"
                  [ div {
                        _class "docs-catalog-grid"

                        catalogLink
                            "/docs/components/layouts"
                            "COMPONENTS"
                            "Browse components"
                            "Layouts, content, navigation, interactive examples, API reference primitives, and diagrams."

                        catalogLink
                            "/docs/page-examples/documentation-site"
                            "PAGE EXAMPLES"
                            "Browse page examples"
                            "Complete documentation, API-reference, and executable-specification compositions."
                    } ] ]

    let layoutsPage =
        // docs-example:start document
        let site =
            DocsSite.create "Acme Docs" "overview"
            |> DocsSite.withAssets
                { DocsAssets.defaults with
                    productStylesheets = [ "/css/output.css" ] }

        let documentPage =
            DocumentationPage.create "overview" "Acme Docs"
            |> DocumentationPage.withDescription "Build and ship with typed documentation."
            |> DocumentationPage.withSections
                [ DocumentationSection.create "welcome" "Welcome" [ p { "Choose a guide to get started." } ] ]

        let documentHtml =
            Document.create site documentPage |> Document.render |> Render.toHtmlDocString
        // docs-example:end document
        registerPreviewPage "/docs/previews/complete-documentation-shell" site documentPage

        // docs-example:start article
        let articlePage =
            DocumentationPage.create "guide" "Getting started"
            |> DocumentationPage.withDescription "Build your first integration."
            |> DocumentationPage.withSections
                [ DocumentationSection.create
                      "install"
                      "Install"
                      [ p { "Add the package to your application." }
                        fveCodeBlock {
                            _language "shell"
                            "dotnet add package Acme"
                        } ] ]
        // docs-example:end article

        // docs-example:start reference
        let referencePage =
            DocumentationPage.create "customers" "Create a customer"
            |> DocumentationPage.withDescription "Customer API reference."
            |> DocumentationPage.withLayout Reference
            |> DocumentationPage.withRightRail (
                CustomRail(ApiReference.codeExample "Request" "curl" "curl -X POST /v1/customers")
            )
            |> DocumentationPage.withSections
                [ DocumentationSection.create
                      "endpoint"
                      "Endpoint"
                      [ ApiEndpoint.create POST "/v1/customers"
                        |> ApiEndpoint.withDescription "Creates a customer."
                        |> ApiEndpoint.render ]
                  DocumentationSection.create
                      "parameters"
                      "Parameters"
                      [ ApiReference.parameters
                            [ ApiReference.parameter "email" "string" true "Customer email address." ] ] ]
        // docs-example:end reference

        // docs-example:start canvas
        let canvasPage =
            DocumentationPage.create "create-view" "Create a view"
            |> DocumentationPage.withDescription "Review the complete workflow."
            |> DocumentationPage.withLayout Canvas
            |> DocumentationPage.withRightRail NoRail
            |> DocumentationPage.withSections
                [ DocumentationSection.create
                      "states"
                      "States"
                      [ fveTabs {
                            _id "view-states"
                            _ariaLabel "View states"
                            _variant TabsVariant.Underlined

                            fveTabsItem {
                                _value "ready"
                                fveTabsTrigger { "Ready" }
                                productScreen "ready"
                            }

                            fveTabsItem {
                                _value "validation"
                                fveTabsTrigger { "Validation" }
                                productScreen "validation"
                            }
                        } ]
                  DocumentationSection.create
                      "sequence"
                      "Sequence"
                      [ fveMermaid { SequenceDiagram.render (sequence ()) } ] ]
        // docs-example:end canvas

        componentPage
            layoutsRegistration
            "Shells and page layouts for guides, references, and wide product review surfaces."
            "layouts"
            "documentation"
            ("document",
             "Document",
             isolatedDocument
                 "Complete documentation shell"
                 (publicUrl $"{layoutsRegistration.path}#document")
                 documentHtml)
            [ componentExample
                  "article"
                  "Page article"
                  "Use the default article layout for guides and conceptual documentation with a readable content column and table of contents."
                  (isolatedPage "Article layout" (publicUrl $"{layoutsRegistration.path}#article") articlePage)
              componentExample
                  "reference"
                  "Page reference"
                  "Use DocumentationPage.withLayout Reference to keep endpoint documentation beside request and response examples."
                  (isolatedPage "Reference layout" (publicUrl $"{layoutsRegistration.path}#reference") referencePage)
              componentExample
                  "canvas"
                  "Page canvas"
                  "Use DocumentationPage.withLayout Canvas for product frames, workflow states, and architecture diagrams; DocumentationPage.withHiddenHeading retains a semantic heading when the framed product already supplies one."
                  (isolatedPage "Canvas layout" (publicUrl $"{layoutsRegistration.path}#canvas") canvasPage) ]

    let contentPage =
        // docs-example:start sections-and-prose
        let prosePage =
            DocumentationPage.create "content-prose" "Content"
            |> DocumentationPage.withDescription "Typed prose blocks."
            |> DocumentationPage.withSections
                [ DocumentationSection.create
                      "install"
                      "Install the package"
                      [ p {
                            _class "m-0 text-base leading-relaxed text-[var(--fve-muted-text)] [overflow-wrap:anywhere]"
                            "Compose readable documentation from typed HTML."
                        }
                        ol {
                            _class
                                "m-0 list-decimal pl-5 text-base leading-relaxed text-[var(--fve-muted-text)] marker:text-[var(--fve-brand-ring)]"

                            li {
                                _class "mt-2 first:mt-0"
                                "Add the package"
                            }

                            li {
                                _class "mt-2 first:mt-0"
                                "Configure assets"
                            }

                            li {
                                _class "mt-2 first:mt-0"
                                "Render the document"
                            }
                        } ] ]
        // docs-example:end sections-and-prose

        // docs-example:start tables
        let tablePage =
            DocumentationPage.create "content-table" "Builder comparison"
            |> DocumentationPage.withDescription "Compact structured data."
            |> DocumentationPage.withSections
                [ DocumentationSection.create
                      "builders"
                      "Builders"
                      [ div {
                            _class "overflow-x-auto rounded-xl border border-[var(--fve-border)]"

                            table {
                                _class "w-full border-collapse text-sm"

                                thead {
                                    tr {
                                        th {
                                            _class
                                                "bg-[var(--fve-surface-subtle)] p-3 text-left text-xs tracking-wide text-[var(--fve-muted-text)] uppercase"

                                            "Builder"
                                        }

                                        th {
                                            _class
                                                "bg-[var(--fve-surface-subtle)] p-3 text-left text-xs tracking-wide text-[var(--fve-muted-text)] uppercase"

                                            "Purpose"
                                        }
                                    }
                                }

                                tbody {
                                    tr {
                                        td {
                                            _class "border-t border-[var(--fve-border)] p-3 align-top"
                                            "DocumentationPage.create"
                                        }

                                        td {
                                            _class "border-t border-[var(--fve-border)] p-3 align-top"
                                            "Guides"
                                        }
                                    }

                                    tr {
                                        td {
                                            _class "border-t border-[var(--fve-border)] p-3 align-top"
                                            "DocumentationPage.withLayout Canvas"
                                        }

                                        td {
                                            _class "border-t border-[var(--fve-border)] p-3 align-top"
                                            "Product review"
                                        }
                                    }
                                }
                            }
                        } ] ]
        // docs-example:end tables

        // docs-example:start callouts
        let calloutPage =
            DocumentationPage.create "content-callout" "Security"
            |> DocumentationPage.withDescription "Important implementation guidance."
            |> DocumentationPage.withSections
                [ DocumentationSection.create
                      "boundary"
                      "Trust boundary"
                      [ fveCallout {
                            _label "Security"

                            p {
                                _class "m-0 leading-relaxed"
                                "Render trusted raw content only at an application-defined boundary."
                            }
                        } ] ]
        // docs-example:end callouts

        // docs-example:start code-and-custom
        let customPage =
            DocumentationPage.create "content-custom" "Output"
            |> DocumentationPage.withDescription "Code and product-owned HTML."
            |> DocumentationPage.withSections
                [ DocumentationSection.create
                      "output"
                      "Output"
                      [ fveCodeBlock {
                            _language "fsharp"
                            "div { _class \"notice\"; \"Saved\" }"
                        }
                        div {
                            _class "docs-notice-preview"
                            "Rendered output"
                        } ] ]
        // docs-example:end code-and-custom

        // docs-example:start step-input
        let stepInput =
            fveField {
                fveLabel {
                    _for "step-name"
                    "Project name"
                }

                fveInput {
                    _id "step-name"
                    _name "step-name"
                }
            }
        // docs-example:end step-input

        // docs-example:start step-input-help
        let stepInputWithHelp =
            fveField {
                fveLabel {
                    _for "step-name-help"
                    "Project name"
                }

                fveInput {
                    _id "step-name-help"
                    _name "step-name-help"
                    _ariaDescribedby (String.concat " " [ ("step-name-help") + "-description" ])
                }

                fveFieldDescription {
                    _id ("step-name-help" + "-description")
                    "Use a name your team recognizes."
                }
            }
        // docs-example:end step-input-help

        // docs-example:start typography
        let typographyExample =
            div {
                _class "prose prose-neutral max-w-none overflow-x-auto break-words dark:prose-invert"
                h3 { "Publishing a guide" }

                p {
                    "Unstyled content can use "
                    strong { "Tailwind Typography" }
                    " without a custom wrapper component."
                }

                ul {
                    li { "Write semantic HTML." }
                    li { "Keep interactive components outside prose or inside not-prose." }
                }

                blockquote { p { "A content boundary is not a component theme." } }

                p {
                    "Use "
                    code { "prose" }
                    " for content and keep "

                    a {
                        _href "/components/installation#tailwind"
                        "Tailwind setup"
                    }

                    " in the host application."
                }

                table {
                    thead {
                        tr {
                            th { "Content" }
                            th { "Presentation" }
                        }
                    }

                    tbody {
                        tr {
                            td { "Article HTML" }
                            td { "prose" }
                        }

                        tr {
                            td { "Form controls" }
                            td { "Component utilities" }
                        }
                    }
                }

                pre { code { "div { _class \"prose dark:prose-invert\"; content }" } }

                div {
                    _class "not-prose grid gap-4"

                    fveField {
                        fveLabel {
                            _for "typography-title"
                            "Document title"
                        }

                        fveInput {
                            _id "typography-title"
                            _name "typography-title"
                            _ariaDescribedby (String.concat " " [ ("typography-title") + "-description" ])
                        }

                        fveFieldDescription {
                            _id ("typography-title" + "-description")
                            "This input keeps its own typography, spacing, and focus treatment."
                        }
                    }

                    fveTable {
                        _ariaLabel "Publishing checklist"
                        fveTableHeader { fveTableRow { fveTableHead { "Step" } } }

                        fveTableBody {
                            for step in [ "Review"; "Publish" ] do
                                fveTableRow {
                                    fveTableCell {
                                        _label "Step"
                                        step
                                    }
                                }
                        }
                    }
                }
            }
        // docs-example:end typography

        componentPage
            contentRegistration
            "Typed blocks for readable prose, structured data, code, and custom composition."
            "content"
            "documentation"
            ("sections-and-prose",
             "Sections, prose, and lists",
             isolatedPage
                 "Sections, prose, and lists"
                 (publicUrl $"{contentRegistration.path}#sections-and-prose")
                 prosePage)
            [ DocumentationSection.create
                  "progressive-examples"
                  "Progressive examples"
                  [ p {
                        _class "m-0 text-base leading-relaxed text-[var(--fve-muted-text)]"
                        "Start with a simple example, then introduce one feature at a time. Each example has its own preview and complete copyable code."
                    }
                    fveExample {
                        _name "docs-step-input"
                        _label "Basic input"
                        _variant ExampleVariant.PreviewFirst
                        _language "fsharp"
                        fveExampleCode { sourceFor "step-input" }
                        stepInput
                    }
                    fveExample {
                        _name "docs-step-input-help"
                        _label "Add help text"
                        _variant ExampleVariant.Gallery
                        _language "fsharp"
                        fveExampleCode { sourceFor "step-input-help" }
                        stepInputWithHelp
                    } ]
              componentExample
                  "typography"
                  "Unstyled content with Tailwind Typography"
                  "Enable @plugin \"@tailwindcss/typography\" in your host stylesheet. The standalone Tailwind CLI bundles the plugin. Apply prose only to content, use dark:prose-invert for dark mode, and exclude components with not-prose. Styling does not sanitize untrusted HTML."
                  typographyExample
              componentExample
                  "tables"
                  "Tables"
                  "Present compact metadata and comparisons with responsive horizontal overflow."
                  (isolatedPage "Tables" (publicUrl $"{contentRegistration.path}#tables") tablePage)
              componentExample
                  "callouts"
                  "Callouts"
                  "Highlight a concise warning, note, or constraint without turning the page into a card grid."
                  (isolatedPage "Callouts" (publicUrl $"{contentRegistration.path}#callouts") calloutPage)
              componentExample
                  "code-and-custom"
                  "Code and custom HTML"
                  "Use CodeBlock for Prism-ready source and compose product-owned typed HTML directly when a page needs custom content."
                  (isolatedPage
                      "Code and custom HTML"
                      (publicUrl $"{contentRegistration.path}#code-and-custom")
                      customPage) ]

    let navigationPage =
        // docs-example:start navigation-tree
        let navigation =
            [ Nav.page "overview" "Overview" "/" "/"
              Nav.group "guides" "Guides" true [ Nav.page "install" "Installation" "/installation" "/installation" ] ]

        let navigationSite =
            { exampleSite with
                navigation = navigation }

        let navigationPreviewPage =
            DocumentationPage.create "install" "Installation"
            |> DocumentationPage.withDescription "Install the package."
            |> DocumentationPage.withSections []

        let navigationPreview =
            Document.create navigationSite navigationPreviewPage
            |> Document.render
            |> Render.toHtmlDocString
        // docs-example:end navigation-tree
        registerPreviewPage "/docs/previews/navigation-tree" navigationSite navigationPreviewPage

        // docs-example:start page-pager
        let pagerPage =
            DocumentationPage.create "usage" "Usage"
            |> DocumentationPage.withDescription "Compose typed HTML."
            |> DocumentationPage.withSections []
            |> DocumentationPage.withPager (
                DocsPager.create
                    (Some(DocsPageLink.create "Installation" "/installation"))
                    (Some(DocsPageLink.create "Content components" "/docs/components/content"))
            )
        // docs-example:end page-pager

        // docs-example:start site-actions
        let siteWithActions =
            { exampleSite with
                defaultColorMode = DocsColorMode.System
                repository = Some(DocsRepository.github "https://github.com/meiermade/FSharp.ViewEngine") }

        let actionsHtml =
            Document.create siteWithActions pagerPage
            |> Document.render
            |> Render.toHtmlDocString
        // docs-example:end site-actions
        registerPreviewPage "/docs/previews/theme-and-repository-actions" siteWithActions pagerPage

        componentPage
            navigationRegistration
            "Discoverable navigation for the complete documentation journey."
            "navigation"
            "documentation"
            ("navigation-tree",
             "Navigation, breadcrumbs, and table of contents",
             isolatedDocument
                 "Navigation tree"
                 (publicUrl $"{navigationRegistration.path}#navigation-tree")
                 navigationPreview)
            [ componentExample
                  "page-pager"
                  "Previous and next"
                  "Add an explicit learning path when the ideal reading order differs from the complete sidebar order."
                  (isolatedPage "Previous and next" (publicUrl $"{navigationRegistration.path}#page-pager") pagerPage)
              componentExample
                  "site-actions"
                  "Theme and repository actions"
                  "Configure System, Light, or Dark as the default and optionally expose a GitHub or custom repository destination."
                  (isolatedDocument
                      "Theme and repository actions"
                      (publicUrl $"{navigationRegistration.path}#site-actions")
                      actionsHtml) ]

    let private fixtureHref state =
        $"/docs/components/fixture?fixtureState={state}"

    let fixturePageFor fixtureStep fixtureState =
        let fixtureStep =
            if fixtureStep = "cart" || fixtureStep = "payment" then
                fixtureStep
            else
                "shipping"

        let fixtureState =
            if fixtureState = "validation" then
                "validation"
            else
                "ready"
        // docs-example:start state-tabs
        let readyView = productScreen "ready"
        let validationView = productScreen "validation"

        let states =
            fveTabs {
                _id "component-workflow-states"
                _ariaLabel "Workflow states"
                _variant TabsVariant.Underlined

                fveTabsItem {
                    _value "ready"
                    fveTabsTrigger { "Ready" }
                    readyView
                }

                fveTabsItem {
                    _value "validation"
                    fveTabsTrigger { "Validation" }
                    validationView
                }
            }
        // docs-example:end state-tabs

        // docs-example:start browser-frame
        let productUi = productView "browser-frame" "ready"

        let browserFramePreview =
            fveBrowser {
                _address (publicUrl (fixtureHref fixtureState))
                productUi
            }
        // docs-example:end browser-frame

        // docs-example:start app-mode
        let appModeStates =
            [ FixtureState.create "Ready" (fixtureHref "ready")
              FixtureState.create "Validation" (fixtureHref "validation") ]
            |> List.mapi (fun index state ->
                if
                    (fixtureState = "ready" && index = 0)
                    || (fixtureState = "validation" && index = 1)
                then
                    FixtureState.current state
                else
                    state)

        let appModeBrowserFixture =
            fveBrowser {
                _address (publicUrl (fixtureHref fixtureState))
                productView "app-mode-browser" fixtureState
            }
            |> Fixture.create "view-studio-browser" "Create a view" (fixtureHref fixtureState)
            |> Fixture.withFullscreenContent (productView "app-mode-browser" fixtureState)
            |> Fixture.withStates appModeStates

        let appModeFixture = appModeBrowserFixture |> Fixture.render
        // docs-example:end app-mode

        // docs-example:start app-mode-phone
        let appModePhoneConfig =
            fvePhone { productView "app-mode-phone" fixtureState }
            |> Fixture.create "view-studio-phone" "Create a view on phone" (fixtureHref fixtureState)

        let appModePhoneFixture = appModePhoneConfig |> Fixture.render
        // docs-example:end app-mode-phone

        // docs-example:start fixture-workflow
        let workflowHref step state =
            $"/docs/components/fixture?fixtureStep={step}&fixtureState={state}"

        let workflowLabel =
            match fixtureStep with
            | "cart" -> "Cart"
            | "payment" -> "Payment"
            | _ -> "Shipping address"

        let workflowContent =
            let heading, readyDescription, validationDescription =
                match fixtureStep with
                | "cart" -> "Your cart", "One item is ready for checkout.", "Review the quantity before continuing."
                | "payment" ->
                    "Payment",
                    "Choose a payment method to place the order.",
                    "Select a payment method before continuing."
                | _ -> "Shipping address", "Enter a delivery address to continue.", "Enter a complete delivery address."

            div {
                _class "grid min-h-64 content-center gap-3 bg-[var(--fve-surface-subtle)] p-8 text-center"

                strong {
                    _class "text-lg"
                    heading
                }

                p {
                    _class "text-sm text-[var(--fve-muted-text)]"

                    if fixtureState = "validation" then
                        validationDescription
                    else
                        readyDescription
                }
            }

        let currentHref = workflowHref fixtureStep fixtureState

        let checkoutFixtureConfig =
            fveBrowser {
                _address ("https://fve.meiermade.com" + currentHref)
                workflowContent
            }
            |> Fixture.create "checkout-workflow" workflowLabel currentHref
            |> Fixture.withFullscreenContent workflowContent
            |> fun fixture ->
                match fixtureStep with
                | "shipping" ->
                    fixture
                    |> Fixture.withPrevious (FixtureLink.create "Cart" (workflowHref "cart" "ready"))
                | "payment" ->
                    fixture
                    |> Fixture.withPrevious (FixtureLink.create "Shipping address" (workflowHref "shipping" "ready"))
                | _ -> fixture
            |> fun fixture ->
                match fixtureStep with
                | "cart" ->
                    fixture
                    |> Fixture.withNext (FixtureLink.create "Shipping address" (workflowHref "shipping" "ready"))
                | "shipping" ->
                    fixture
                    |> Fixture.withNext (FixtureLink.create "Payment" (workflowHref "payment" "ready"))
                | _ -> fixture
            |> Fixture.withStates
                [ FixtureState.create "Ready" (workflowHref fixtureStep "ready")
                  |> fun state ->
                      if fixtureState = "ready" then
                          FixtureState.current state
                      else
                          state
                  FixtureState.create "Validation" (workflowHref fixtureStep "validation")
                  |> fun state ->
                      if fixtureState = "validation" then
                          FixtureState.current state
                      else
                          state ]

        let checkoutFixture = checkoutFixtureConfig |> Fixture.render
        // docs-example:end fixture-workflow

        componentPage
            fixtureRegistration
            "Compose arbitrary HTML review fixtures with optional caller-rendered Browser or Phone frames, source, preview, and copyable code."
            "fixture"
            "documentation-fixture"
            ("state-tabs", "Review states", previewSurface states)
            [ componentExample
                  "browser-frame"
                  "Browser frame"
                  "Place product UI in a browser-like frame with an explicit canonical URL before optionally enabling App mode."
                  (previewSurface browserFramePreview)
              componentExample
                  "app-mode"
                  "Browser fixture"
                  "Expand a named Browser fixture to a focused executable preview. Fixture supplies independent review states while the product owns their meaning."
                  (previewSurface appModeFixture)
              componentExample
                  "app-mode-phone"
                  "Phone fixture"
                  "Pass a rendered Phone to the same Fixture contract; the consumer owns both framing and screen content."
                  (previewSurface appModePhoneFixture)
              componentExample
                  "fixture-workflow"
                  "Workflow fixture"
                  "Fixture supplies authored previous and next destinations independently from review states. Each destination renders a real server-owned workflow step on this route."
                  (previewSurface checkoutFixture) ]
        |> DocumentationPage.withFixtures [ appModeBrowserFixture; appModePhoneConfig; checkoutFixtureConfig ]

    let apiComponentsPage =
        // docs-example:start api-endpoint
        let endpoint =
            ApiEndpoint.create POST "/v1/customers"
            |> ApiEndpoint.withDescription "Creates a customer and returns its identifier."
            |> ApiEndpoint.render
        // docs-example:end api-endpoint

        // docs-example:start api-parameters
        let parameters =
            ApiReference.parameters
                [ ApiReference.parameter "email" "string" true "Customer email address."
                  ApiReference.parameter "metadata" "object" false "Application-defined values." ]
        // docs-example:end api-parameters

        // docs-example:start api-examples
        let requestSource = "curl -X POST $API_ORIGIN/v1/customers"
        let responseSource = "{ \"id\": \"cus_123\" }"

        let requestResponse =
            div {
                _class "docs-showcase-panels"
                ApiReference.codeExample "Create customer" "curl" requestSource
                ApiReference.responseExample "201" "json" responseSource
            }
        // docs-example:end api-examples

        componentPage
            apiComponentsRegistration
            "Composable endpoint, parameter, request, and response primitives for product-owned APIs."
            "api-reference"
            "documentation-api-reference"
            ("api-endpoint", "Endpoint", previewSurface endpoint)
            [ componentExample
                  "api-parameters"
                  "Parameters"
                  "Describe required and optional values with compact names, types, and explanations."
                  (previewSurface parameters)
              componentExample
                  "api-examples"
                  "Request and response examples"
                  "Keep realistic request source and response payloads visually paired."
                  (previewSurface requestResponse) ]

    let diagramsPage =
        // docs-example:start mermaid
        let flowchart =
            """flowchart LR
    Developer[Developer] --> View[Typed view]
    View --> HTML[Encoded HTML]"""

        let mermaidPreview = fveMermaid { flowchart }
        // docs-example:end mermaid

        // docs-example:start c4
        let c4Source =
            """C4Context
    title Documentation context
    Person(dev, "Developer", "Authors documentation")
    System(docs, "Docs site", "Publishes documentation")
    Rel(dev, docs, "Uses")"""

        let c4Preview =
            fveMermaid {
                _c4 true
                c4Source
            }
        // docs-example:end c4

        // docs-example:start sequence-diagram
        let developer = SequenceDiagram.participant "Developer" "Developer"
        let engine = SequenceDiagram.participant "Engine" "View engine"

        let sequenceDiagram =
            SequenceDiagram.sequence
                [ developer; engine ]
                [ SequenceDiagram.call developer engine "Render view"
                  SequenceDiagram.reply engine developer "HTML" ]

        let sequencePreview = fveMermaid { SequenceDiagram.render sequenceDiagram }
        // docs-example:end sequence-diagram

        componentPage
            diagramsRegistration
            "Trusted Mermaid, C4, and validated sequence diagrams for architecture and workflow communication."
            "diagrams"
            "documentation"
            ("mermaid", "Mermaid", previewSurface mermaidPreview)
            [ componentExample
                  "c4"
                  "C4"
                  "Use Mermaid C4 syntax for a proportionate system context, container, component, dynamic, or deployment view."
                  (previewSurface c4Preview)
              componentExample
                  "sequence-diagram"
                  "Sequence diagram"
                  "Construct participants and calls with the validated sequence DSL before rendering Mermaid."
                  (previewSurface sequencePreview) ]

    let private documentationOverviewPreviewPath =
        "/docs/previews/documentation-site-overview"

    let private documentationGettingStartedPreviewPath =
        "/docs/previews/documentation-site-getting-started"

    let documentationSitePageFor stateValue =
        let current =
            if stateValue = "overview" then
                "overview"
            else
                "getting-started"

        // docs-example:start documentation-site-page
        let overviewPage =
            DocumentationPage.create "overview" "Acme Docs"
            |> DocumentationPage.withDescription "Build and ship a reliable integration."
            |> DocumentationPage.withSections
                [ DocumentationSection.create
                      "start"
                      "Start building"
                      [ p { "Install FSharp.ViewEngine, compose typed HTML, and render your first view." }
                        fveCodeBlock {
                            _language "shell"
                            "dotnet add package FSharp.ViewEngine"
                        } ] ]

        let greeting =
            div {
                h3 { "Hello from F#" }
                p { "Typed HTML rendered on the server." }
            }

        let greetingSource =
            "open FSharp.ViewEngine\nopen type Html\n\nlet greeting = div { h3 { \"Hello from F#\" }; p { \"Typed HTML rendered on the server.\" } }\nlet html = Render.toString greeting"

        let gettingStartedPage =
            DocumentationPage.create "guide" "Getting started"
            |> DocumentationPage.withDescription "Install the view engine, compose a typed view, and inspect its HTML."
            |> DocumentationPage.withSections
                [ DocumentationSection.create
                      "install"
                      "Install"
                      [ fveCodeBlock {
                            _language "shell"
                            "dotnet add package FSharp.ViewEngine"
                        } ]
                  DocumentationSection.create
                      "compose"
                      "Compose a view"
                      [ fveCodeBlock {
                            _language "fsharp"
                            greetingSource
                        } ]
                  DocumentationSection.create
                      "output"
                      "Rendered output"
                      [ p {
                            "Render.toString serializes the typed view. Return this string as HTML from your web application; the engine does not bundle styles."
                        }
                        div {
                            _class "rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] p-4"
                            greeting
                        }
                        fveCodeBlock {
                            _language "html"
                            Render.toString greeting
                        } ] ]
            |> DocumentationPage.withPager (DocsPager.create (Some(DocsPageLink.create "Overview" "/")) None)
        // docs-example:end documentation-site-page

        let fixtureSite =
            { exampleSite with
                navigation =
                    [ Nav.group
                          "guides"
                          "Guides"
                          true
                          [ Nav.page
                                "overview"
                                "Overview"
                                documentationOverviewPreviewPath
                                documentationOverviewPreviewPath
                            Nav.page
                                "guide"
                                "Getting started"
                                documentationGettingStartedPreviewPath
                                documentationGettingStartedPreviewPath ] ] }

        let fixtureGettingStartedPage =
            gettingStartedPage
            |> DocumentationPage.withPager (
                DocsPager.create (Some(DocsPageLink.create "Overview" documentationOverviewPreviewPath)) None
            )

        registerPreviewPage documentationOverviewPreviewPath fixtureSite overviewPage
        registerPreviewPage documentationGettingStartedPreviewPath fixtureSite fixtureGettingStartedPage

        let states =
            [ "overview", "Overview", documentationSiteRegistration.path + "?fixtureState=overview"
              "getting-started", "Getting started", documentationSiteRegistration.path + "?fixtureState=getting-started" ]

        let previewPath =
            if current = "overview" then
                documentationOverviewPreviewPath
            else
                documentationGettingStartedPreviewPath

        let canonicalUrl = publicUrl previewPath

        let fixture =
            statefulDocumentFixture
                "documentation-site-page-example"
                "Documentation site page example"
                (documentationSiteRegistration.path + "?fixtureState=" + current)
                canonicalUrl
                previewPath
                current
                states

        let preview =
            statefulDocumentFrame fixture "Documentation site review state" current states

        DocumentationPage.create documentationSiteRegistration.id documentationSiteRegistration.title
        |> DocumentationPage.withDescription
            "A complete guide composition using the shared shell, navigation, content, examples, and pager."
        |> DocumentationPage.withLayout Gallery
        |> DocumentationPage.withRightRail NoRail
        |> DocumentationPage.withFixtures [ fixture ]
        |> DocumentationPage.withSections
            [ DocumentationSection.create
                  "documentation-site-page"
                  "Guide with navigation"
                  [ fveExample {
                        _name "docs-documentation-site-page-example"
                        _label "Guide with navigation"
                        _variant ExampleVariant.Gallery
                        _language "fsharp"
                        fveExampleCode { sourceFor "documentation-site-page" }
                        preview
                    } ]
              DocumentationSection.create
                  "building-blocks"
                  "Built with"
                  [ buildingBlockLinks
                        "Documentation site building blocks"
                        [ layoutsRegistration.path, "Layouts"
                          contentRegistration.path, "Content"
                          navigationRegistration.path, "Navigation" ] ] ]

    let documentationSitePage = documentationSitePageFor "getting-started"

    let private apiOverviewPreviewPath = "/docs/previews/api-reference-overview"
    let private apiRenderPreviewPath = "/docs/previews/api-reference-render-view"

    let apiPageExampleFor stateValue =
        let current =
            if stateValue = "overview" then
                "overview"
            else
                "render-view"

        // docs-example:start api-reference-page
        let viewParameter =
            ApiParameter.create "view" "string" Body
            |> ApiParameter.required
            |> ApiParameter.withDescription "Typed view source to encode and render."
            |> ApiParameter.withExample "main { h1 { \"Hello\" } }"

        let contentTypeParameter =
            ApiParameter.create "content_type" "string" Body
            |> ApiParameter.withDescription "Response media type."
            |> ApiParameter.withDefaultValue "text/html"
            |> ApiParameter.withEnumValues [ "text/html"; "application/xhtml+xml" ]

        let prettyParameter =
            ApiParameter.create "pretty" "boolean" Query
            |> ApiParameter.withDescription "Indent the returned markup for inspection."
            |> ApiParameter.withDefaultValue "false"

        let successResponse =
            ApiResponse.create "200"
            |> ApiResponse.withDescription "Rendered HTML and response media type."

        let operation =
            ApiOperation.create POST "/v1/render"
            |> ApiOperation.withDescription "Renders typed view source into encoded HTML."
            |> ApiOperation.withAuthentication "Send a bearer token in the Authorization header."
            |> ApiOperation.withApiVersion "2026-09-01"
            |> ApiOperation.withIdempotency "Repeated requests with the same key return the original result."
            |> ApiOperation.withParameters [ viewParameter; contentTypeParameter; prettyParameter ]
            |> ApiOperation.withResponses [ successResponse ]
            |> ApiOperation.withErrors
                [ ApiError.create "invalid_view" "The supplied view could not be parsed."
                  ApiError.create "unsupported_content_type" "The requested response type is unavailable." ]

        let requestExample =
            """curl $API_ORIGIN/v1/render \
  -H "Authorization: Bearer $ACME_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"view":"main { h1 { \"Hello\" } }"}'"""

        let responseExample =
            """{
  "html": "<main><h1>Hello</h1></main>",
  "content_type": "text/html"
}"""

        let operationRail =
            div {
                div {
                    _class "hidden xl:block"
                    ApiReference.codeExample "Request" "curl" requestExample
                }

                ApiReference.responseExample "200" "json" responseExample
            }

        let apiPage =
            DocumentationPage.create "render" "Render a view"
            |> DocumentationPage.withDescription "Render typed HTML safely at the service boundary."
            |> DocumentationPage.withLayout Reference
            |> DocumentationPage.withRightRail (CustomRail operationRail)
            |> DocumentationPage.withSections
                [ DocumentationSection.create
                      "request"
                      "Request"
                      [ div {
                            _class "xl:hidden"
                            ApiReference.codeExample "Request" "curl" requestExample
                        }
                        ApiOperation.render operation ] ]

        let overviewRail = ApiReference.codeExample "View object" "json" responseExample

        let apiOverviewPage =
            DocumentationPage.create "api-overview" "Rendering API"
            |> DocumentationPage.withDescription "Convert typed view source into deterministic HTML."
            |> DocumentationPage.withLayout Reference
            |> DocumentationPage.withRightRail (CustomRail overviewRail)
            |> DocumentationPage.withSections
                [ DocumentationSection.create
                      "operations"
                      "Operations"
                      [ nav {
                            _ariaLabel "Rendering operations"
                            _class "docs-api-operation-links"

                            a {
                                _href apiRenderPreviewPath
                                strong { "Render a view" }

                                span {
                                    _class "docs-api-operation-link-path"

                                    span {
                                        _class "docs-http-method docs-method-post"
                                        "POST"
                                    }

                                    code { "/v1/render" }
                                }
                            }
                        } ]
                  DocumentationSection.create
                      "view-object"
                      "View object"
                      [ ApiReference.parameters
                            [ ApiReference.parameter "html" "string" true "Encoded HTML returned by the renderer."
                              ApiReference.parameter "content_type" "string" true "Media type of the rendered response." ] ] ]
        // docs-example:end api-reference-page

        let apiSite =
            { exampleSite with
                name = "Acme API"
                homeId = "api-overview"
                navigation =
                    [ Nav.group
                          "api-reference"
                          "API reference"
                          true
                          [ Nav.page "api-overview" "Overview" apiOverviewPreviewPath apiOverviewPreviewPath
                            Nav.group
                                "rendering"
                                "Rendering"
                                true
                                [ Nav.page "render" "Render a view" apiRenderPreviewPath apiRenderPreviewPath ] ] ] }

        registerPreviewPage apiOverviewPreviewPath apiSite apiOverviewPage
        registerPreviewPage apiRenderPreviewPath apiSite apiPage

        let states =
            [ "overview", "Overview", apiPageExampleRegistration.path + "?fixtureState=overview"
              "render-view", "Render a view", apiPageExampleRegistration.path + "?fixtureState=render-view" ]

        let previewPath =
            if current = "overview" then
                apiOverviewPreviewPath
            else
                apiRenderPreviewPath

        let canonicalUrl = publicUrl previewPath

        let fixture =
            statefulDocumentFixture
                "api-reference-page-example"
                "API reference page example"
                (apiPageExampleRegistration.path + "?fixtureState=" + current)
                canonicalUrl
                previewPath
                current
                states

        let preview =
            statefulDocumentFrame fixture "API reference review state" current states

        DocumentationPage.create apiPageExampleRegistration.id apiPageExampleRegistration.title
        |> DocumentationPage.withDescription
            "A resource overview and operation reference with request and response examples."
        |> DocumentationPage.withLayout Gallery
        |> DocumentationPage.withRightRail NoRail
        |> DocumentationPage.withFixtures [ fixture ]
        |> DocumentationPage.withSections
            [ DocumentationSection.create
                  "api-reference-page"
                  "Rendering API reference"
                  [ fveExample {
                        _name "docs-api-reference-page-example"
                        _label "Rendering API reference"
                        _variant ExampleVariant.Gallery
                        _language "fsharp"
                        fveExampleCode { sourceFor "api-reference-page" }
                        preview
                    }
                    p {
                        "FSharp.ViewEngine does not expose an HTTP /v1/render endpoint. This fictional operation keeps the example focused on the reference layout and reusable API components."
                    } ]
              DocumentationSection.create
                  "building-blocks"
                  "Built with"
                  [ buildingBlockLinks
                        "API reference building blocks"
                        [ layoutsRegistration.path, "Layouts"
                          apiComponentsRegistration.path, "API reference" ] ] ]

    let apiPageExample = apiPageExampleFor "render-view"

    let private specificationOverviewPreviewPath =
        "/docs/previews/executable-specification-overview"

    let private specificationRenderPreviewPath =
        "/docs/previews/executable-specification-render-view"

    let specificationPageExample =
        // docs-example:start executable-specification-page
        let readyView = productScreen "ready"
        let validationView = productScreen "validation"

        let states =
            fveTabs {
                _id "page-example-render-states"
                _ariaLabel "Create-view workflow states"
                _variant TabsVariant.Underlined

                fveTabsItem {
                    _value "ready"
                    fveTabsTrigger { "Ready" }
                    readyView
                }

                fveTabsItem {
                    _value "validation"
                    fveTabsTrigger { "Validation" }
                    validationView
                }
            }

        let author = SequenceDiagram.participant "Author" "Author"
        let form = SequenceDiagram.participant "Form" "Create view form"
        let workspace = SequenceDiagram.participant "Workspace" "Workspace"

        let workflowSequence =
            SequenceDiagram.sequence
                [ author; form; workspace ]
                [ SequenceDiagram.call author form "Enter view name"
                  SequenceDiagram.call author form "Create view"
                  SequenceDiagram.call form form "Validate nonblank name"
                  SequenceDiagram.call form workspace "Create named view"
                  SequenceDiagram.reply workspace author "Show the new view" ]

        let specificationPage =
            DocumentationPage.create "render-workflow" "Create a view"
            |> DocumentationPage.withDescription
                "A proposed create-view workflow with static Ready and Validation wireframes. Product controls are disabled."
            |> DocumentationPage.withLayout Canvas
            |> DocumentationPage.withRightRail NoRail
            |> DocumentationPage.withSections
                [ DocumentationSection.create "wireframe" "Wireframe" [ states ]
                  DocumentationSection.create
                      "sequence"
                      "Sequence"
                      [ fveMermaid { SequenceDiagram.render workflowSequence } ]
                  DocumentationSection.create
                      "rules"
                      "Rules"
                      [ ul {
                            li { "A blank name stays on the form and shows 'Enter a view name.'" }
                            li { "A valid name creates a named view and returns to the workspace." }
                            li { "These wireframes compare presentation states; they do not submit or store data." }
                        } ] ]
        // docs-example:end executable-specification-page

        let overviewPage =
            DocumentationPage.create "specification-overview" "Creating a view"
            |> DocumentationPage.withDescription "Review the proposed form, sequence, and validation rules."
            |> DocumentationPage.withLayout Canvas
            |> DocumentationPage.withRightRail NoRail
            |> DocumentationPage.withSections
                [ DocumentationSection.create
                      "workflow"
                      "Workflow"
                      [ p {
                            "Inspect the static wireframes, sequence, and acceptance rules for naming and creating a view."
                        }
                        a {
                            _href specificationRenderPreviewPath
                            "Create a view"
                        } ] ]

        let specificationSite =
            { exampleSite with
                homeId = "specification-overview"
                navigation =
                    [ Nav.group
                          "specification"
                          "Specification"
                          true
                          [ Nav.page
                                "specification-overview"
                                "Overview"
                                specificationOverviewPreviewPath
                                specificationOverviewPreviewPath
                            Nav.page
                                "render-workflow"
                                "Create a view"
                                specificationRenderPreviewPath
                                specificationRenderPreviewPath ] ] }

        registerPreviewPage specificationOverviewPreviewPath specificationSite overviewPage
        registerPreviewPage specificationRenderPreviewPath specificationSite specificationPage

        let fixture =
            browserConfig
                "Executable specification page example"
                (publicUrl specificationRenderPreviewPath)
                specificationRenderPreviewPath
            |> Fixture.create
                "executable-specification-page-example"
                "Executable specification page example"
                specificationPageExampleRegistration.path
            |> Fixture.withFullscreenContent (
                previewContent "Executable specification page example" specificationRenderPreviewPath true
            )

        let preview =
            div {
                _data ("fve-full-bleed-example", "true")
                fixture |> Fixture.render
            }

        DocumentationPage.create specificationPageExampleRegistration.id specificationPageExampleRegistration.title
        |> DocumentationPage.withDescription
            "A complete workflow review composition using a canvas, browser frames, tabs, diagrams, and rules."
        |> DocumentationPage.withLayout Gallery
        |> DocumentationPage.withRightRail NoRail
        |> DocumentationPage.withFixtures [ fixture ]
        |> DocumentationPage.withSections
            [ DocumentationSection.create
                  "executable-specification-page"
                  "Create-view workflow"
                  [ fveExample {
                        _name "docs-executable-specification-page-example"
                        _label "Create-view workflow"
                        _variant ExampleVariant.Gallery
                        _language "fsharp"
                        fveExampleCode { sourceFor "executable-specification-page" }
                        preview
                    } ]
              DocumentationSection.create
                  "building-blocks"
                  "Built with"
                  [ buildingBlockLinks
                        "Executable specification building blocks"
                        [ layoutsRegistration.path, "Layouts"
                          "/components/browser", "Browser"
                          "/components/tabs", "Tabs"
                          diagramsRegistration.path, "Diagrams" ] ] ]

    let private pages =
        [ overviewRegistration.path, overviewPage
          layoutsRegistration.path, layoutsPage
          contentRegistration.path, contentPage
          navigationRegistration.path, navigationPage
          fixtureRegistration.path, fixturePageFor "shipping" "ready"
          apiComponentsRegistration.path, apiComponentsPage
          diagramsRegistration.path, diagramsPage
          documentationSiteRegistration.path, documentationSitePage
          apiPageExampleRegistration.path, apiPageExample
          specificationPageExampleRegistration.path, specificationPageExample ]
        |> Map.ofList

    let previewRoutes =
        previewDocuments |> Seq.map (fun pair -> pair.Key, pair.Value) |> Map.ofSeq

    let tryPage path = Map.tryFind path pages
