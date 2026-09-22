module DocsTests

open System.IO
open Expecto
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Primitives
open FSharp.ViewEngine.Components.Documentation
open type Html

type Destination =
    | Home
    | Guide
    | Detail
    | Reference

let private issueCodes (issues:ValidationIssue list) = issues |> List.map _.code |> Set.ofList

let private docsTailwindManifest () =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "FSharp.ViewEngine.Components", "Documentation", "Documentation.tailwind.css"))
    |> File.ReadAllText

let private sequenceDiagram () =
    let person = SequenceDiagram.participant "Person" "Person"
    let app = SequenceDiagram.participant "App" "Application"
    SequenceDiagram.sequence [ person; app ] [ SequenceDiagram.call person app "Use product" ]

let private navigation =
    [ Nav.page "home" "Overview" "/" Home
      Nav.groupWithBreadcrumb "guides" "Guides" "/guides" true [
          Nav.page "guide" "Overview" "/guides" Guide
          Nav.page "detail" "Detail" "/guides/detail" Detail ]
      Nav.page "reference" "API reference" "/reference" Reference ]

let private site: DocsSite<Destination> =
    { name = "Example"
      baseUrl = Some "https://docs.example.com"
      description = Some "Example product documentation."
      repository = Some(DocsRepository.github "https://github.com/example/docs")
      brandMark = span { _ariaHidden "true"; "E" }
      homeId = "home"
      navigation = navigation
      storageKey = "example-docs-navigation"
      defaultColorMode = DocsColorMode.System
      theme = DocsTheme.amber
      assets = DocsAssets.defaults
      search = [] }

[<Tests>]
let tests =
    testList "Docs Tests" [
        test "Sequence diagrams render nested, validated Mermaid" {
            let person = SequenceDiagram.participant "Person" "Person"
            let app = SequenceDiagram.participant "App" "Application"
            let api = SequenceDiagram.participant "Api" "HTTP API"

            let diagram =
                SequenceDiagram.sequence
                    [ person; app; api ]
                    [ SequenceDiagram.call person app "Submit form"
                      SequenceDiagram.optional "Valid input" [
                          SequenceDiagram.call app api "Create item"
                          SequenceDiagram.alternatives [
                              SequenceDiagram.branch "Created" [ SequenceDiagram.reply api app "201 Created" ]
                              SequenceDiagram.branch "Rejected" [ SequenceDiagram.reply api app "422 Validation error" ] ] ] ]

            let rendered = SequenceDiagram.render diagram
            Expect.stringStarts rendered "sequenceDiagram\n    autonumber" "diagram header"
            Expect.stringContains rendered "alt Created" "first alternative"
            Expect.stringContains rendered "else Rejected" "remaining alternative"
            Expect.stringContains rendered "Api-->>App: 422 Validation error" "reply"
        }

        test "Sequence diagrams reject undeclared participants" {
            let declared = SequenceDiagram.participant "Declared" "Declared"
            let missing = SequenceDiagram.participant "Missing" "Missing"

            Expect.throwsT<System.ArgumentException>
                (fun () ->
                    SequenceDiagram.sequence [ declared ] [ SequenceDiagram.call declared missing "Call" ]
                    |> ignore)
                "all referenced participants must be declared"
        }

        test "Targets encode query values and fragments" {
            let target =
                Target.create Detail
                |> Target.withQuery "return to" "/items?status=open"
                |> Target.withFragment "validation errors"

            let actual = Target.href (function Detail -> "/guides/detail" | _ -> "/") target
            Expect.equal actual "/guides/detail?return%20to=%2Fitems%3Fstatus%3Dopen#validation%20errors" "target URL"
        }

        test "Navigation builders support pages, groups, and optional breadcrumb links" {
            let group = navigation[1]
            Expect.isNone (NavNode.destination group) "groups have no typed page destination"
            Expect.equal (NavNode.breadcrumbHref group) (Some "/guides") "breadcrumb destination is optional metadata"

            let breadcrumbs = Navigation.breadcrumbs navigation "home" "detail"
            Expect.sequenceEqual
                (breadcrumbs |> List.map (fun breadcrumb -> breadcrumb.label, breadcrumb.href))
                [ "Home", Some "/"; "Guides", Some "/guides"; "Detail", Some "/guides/detail" ]
                "breadcrumbs use configured group destinations"
        }

        test "Whole-site validation checks registered routes, reachability, aliases, pagers, and metadata" {
            let guide = DocumentationPage.create "guide" "Guide" |> DocumentationPage.withDescription "Guide description" |> DocumentationPage.withSections [] |> DocumentationPage.withPager (DocsPager.create None (Some(DocsPageLink.create "Missing" "/missing")))
            let orphan = DocumentationPage.create "orphan" "Orphan" |> DocumentationPage.withDescription "" |> DocumentationPage.withSections []
            let registry = [ DocsRegisteredPage.create "/guide" [ "/start" ] guide; DocsRegisteredPage.create "/orphan" [ "/guide" ] orphan ]
            let issues = DocsRegistry.validate navigation registry
            let codes = issueCodes issues

            Expect.isTrue (Set.contains "registry.missing-navigation-page" codes) "navigation pages must be registered"
            Expect.isTrue (Set.contains "registry.unreachable-page" codes) "registered pages must be reachable"
            Expect.isTrue (Set.contains "registry.alias-collision" codes) "aliases cannot collide with canonical routes"
            Expect.isTrue (Set.contains "registry.invalid-pager-target" codes) "pager destinations must resolve"
            Expect.isTrue (Set.contains "page.missing-description" codes) "pages need search and metadata descriptions"
        }

        test "Navigation validation remains structural rather than prescribing hierarchy" {
            let customNavigation =
                [ Nav.page "home" "Start" "/" Home
                  Nav.group "commands" "Commands" false [
                      Nav.page "run" "Run" "/commands/run" Guide ] ]

            Expect.isEmpty (Navigation.validate customNavigation) "groups do not require overview pages or breadcrumb links"

            let invalid =
                [ Nav.page "home" "Home" "/" Home
                  Nav.page "home" "Duplicate" "/" Guide
                  Nav.group "empty" "Empty" false [] ]

            let codes = Navigation.validate invalid |> issueCodes
            Expect.isTrue (Set.contains "navigation.duplicate-id" codes) "duplicate IDs"
            Expect.isTrue (Set.contains "navigation.duplicate-href" codes) "duplicate hrefs"
            Expect.isTrue (Set.contains "navigation.empty-group" codes) "empty groups"
        }

        test "Direct section content renders semantic paragraphs, lists, tables, and callouts" {
            let page =
                DocumentationPage.create "guide" "Guide" |> DocumentationPage.withDescription "Description" |> DocumentationPage.withSections [
                    DocumentationSection.create "content" "Content" [
                        p { _class "spec-paragraph"; "Read "; strong { "carefully" }; " and "; a { _href "/next"; _class "spec-content-link"; "continue" }; " with "; code { "DocumentationPage.create" } }
                        ul { _class "spec-bullets list-disc"; li { "Plain list item" } }
                        ol { _class "spec-bullets list-decimal"; li { "Ordered item" } }
                        div { _class "spec-table-wrap"; table { _class "spec-table"; thead { tr { th { "Builder" }; th { "Purpose" } } }; tbody { tr { td { code { "DocumentationPage.create" } }; td { "Guides" } } } } }
                        Callout.create "Note" [ p { "Direct typed HTML stays semantic." } ] |> Callout.render ] ]

            let rendered = DocumentationPage.render page |> Render.toString
            Expect.stringContains rendered "<strong>carefully</strong>" "strong inline content"
            Expect.stringContains rendered "href=\"/next\"" "inline link"
            Expect.stringContains rendered "<code>DocumentationPage.create</code>" "inline code"
            Expect.stringContains rendered "<ul class=\"spec-bullets list-disc\"><li>Plain list item</li></ul>" "plain bullets restore markers after Preflight"
            Expect.stringContains rendered "<ol class=\"spec-bullets list-decimal\"" "ordered list restores numbers after Preflight"
            Expect.stringContains rendered "<th>Builder</th>" "semantic table header"
            Expect.stringContains rendered "class=\"spec-callout-label\">Note" "callout label"
        }

        test "Heading adornments preserve the semantic page heading" {
            let page =
                DocumentationPage.create "home" "Home" |> DocumentationPage.withDescription "Description" |> DocumentationPage.withSections []
                |> DocumentationPage.withHeadingAdornment (img { _src "/logo.svg"; _alt "" })
            let rendered = DocumentationPage.render page |> Render.toString

            Expect.stringContains rendered "src=\"/logo.svg\"" "adornment is rendered before the title"
            Expect.equal (rendered.Split("<h1").Length - 1) 1 "page retains one semantic h1"
        }

        test "Article, reference, and canvas builders select composable layouts" {
            let sections = [ DocumentationSection.create "usage" "Usage" [ CodeBlock.create "shell" "example run" |> CodeBlock.render; p { _class "spec-paragraph"; "Any page shape is allowed." } ] ]
            let article = DocumentationPage.create "guide" "CLI guide" |> DocumentationPage.withDescription "Run the CLI." |> DocumentationPage.withSections sections
            let reference = DocumentationPage.create "reference" "Create item" |> DocumentationPage.withDescription "Creates an item." |> DocumentationPage.withLayout DocsLayout.Reference |> DocumentationPage.withRightRail (CustomRail (div { "Request examples" })) |> DocumentationPage.withSections sections
            let canvas = DocumentationPage.create "detail" "Architecture" |> DocumentationPage.withDescription "Explore the system." |> DocumentationPage.withLayout Canvas |> DocumentationPage.withRightRail NoRail |> DocumentationPage.withSections sections
            let hiddenCanvas = DocumentationPage.create "detail" "Web workflow" |> DocumentationPage.withDescription "Use the web application." |> DocumentationPage.withLayout Canvas |> DocumentationPage.withRightRail NoRail |> DocumentationPage.withHiddenHeading |> DocumentationPage.withSections sections

            Expect.equal article.layout Article "article layout"
            match article.rightRail with
            | TableOfContents -> ()
            | _ -> failtest "articles should use a table of contents"
            Expect.equal reference.layout DocsLayout.Reference "reference layout"
            Expect.equal canvas.layout Canvas "canvas layout"
            Expect.equal canvas.heading Visible "general canvases retain a visible heading"
            Expect.equal hiddenCanvas.heading VisuallyHidden "consumers may opt into a visually hidden heading"
            Expect.isEmpty (DocsPage.validate reference) "arbitrary valid sections pass structural validation"
        }

        test "Article pages support explicit previous and next navigation" {
            let pager =
                DocsPager.create
                    (Some(DocsPageLink.create "Introduction" "/"))
                    (Some(DocsPageLink.create "Usage" "/usage"))

            let page =
                DocumentationPage.create "guide" "Guide" |> DocumentationPage.withDescription "Description" |> DocumentationPage.withSections []
                |> DocumentationPage.withPager pager

            let rendered = Document.create site page |> Document.render |> Render.toString
            Expect.stringContains rendered "aria-label=\"Page navigation\"" "pager landmark"
            Expect.stringContains rendered "rel=\"prev\" href=\"/\"" "previous destination"
            Expect.stringContains rendered "rel=\"next\" href=\"/usage\"" "next destination"
            Expect.stringContains rendered "Previous" "previous direction"
            Expect.stringContains rendered ">Introduction</span>" "previous page title"
            Expect.stringContains rendered "Next" "next direction"
            Expect.stringContains rendered ">Usage</span>" "next page title"
            Expect.stringContains rendered "window.fsharpDocsNavigation.navigate(evt" "pager uses Docs navigation lifecycle"
            Expect.stringContains rendered "fsharpdocs:navigate" "pager requests standard Datastar navigation"
        }

        test "Search indexes include page titles, headings, descriptions, and consumer keywords" {
            let page = DocumentationPage.create "guide" "Build a view" |> DocumentationPage.withDescription "Compose typed HTML." |> DocumentationPage.withSections [ DocumentationSection.create "render" "Render output" [] ]
            let entry = DocsSearchEntry.create "/guide" page [ "serialization" ]
            let index = DocsSearch.index [ entry ]
            let rendered = SearchView.render index |> Render.toString

            Expect.sequenceEqual index[0].keywords [ "serialization" ] "consumer keywords"
            Expect.stringContains rendered "aria-label=\"Search documentation\"" "search dialog label"
            Expect.stringContains rendered "data-docs-search-entry" "search result metadata"
            Expect.stringContains rendered "href=\"/guide#render\"" "heading deep link"
            Expect.stringContains rendered "Ctrl+K" "keyboard shortcut hint"
        }

        test "Page metadata controls browser, search, and social metadata" {
            let page =
                DocumentationPage.create "guide" "Guide" |> DocumentationPage.withDescription "A focused guide description." |> DocumentationPage.withSections []
                |> DocumentationPage.withMetadata {
                    DocsPageMetadata.defaults with
                        browserTitle = Some "Guide · Example"
                        canonicalUrl = Some "https://docs.example.com/canonical-guide"
                        noIndex = true
                        socialImage = Some "https://docs.example.com/guide.png"
                        version = Some "2026.8"
                        deprecated = true
                        lastUpdated = Some "2026-08-12"
                        editUrl = Some "https://github.com/example/docs/edit/main/guide.fs" }

            let rendered = Document.create site page |> Document.render |> Render.toString
            Expect.stringContains rendered "<title>Guide &#183; Example</title>" "browser title"
            Expect.stringContains rendered "name=\"description\" content=\"A focused guide description.\"" "page description"
            Expect.stringContains rendered "rel=\"canonical\" href=\"https://docs.example.com/canonical-guide\"" "canonical override"
            Expect.stringContains rendered "name=\"robots\" content=\"noindex\"" "robots metadata"
            Expect.stringContains rendered "property=\"og:url\" content=\"https://docs.example.com/canonical-guide\"" "Open Graph canonical URL"
            Expect.stringContains rendered "property=\"og:type\" content=\"website\"" "Open Graph type"
            Expect.stringContains rendered "property=\"og:site_name\" content=\"Example\"" "Open Graph site name"
            Expect.stringContains rendered "property=\"og:image\" content=\"https://docs.example.com/guide.png\"" "social image"
            Expect.stringContains rendered "property=\"og:image:alt\" content=\"Guide\"" "social image alternative"
            Expect.stringContains rendered "data-docs-version=\"2026.8\"" "version metadata"
            Expect.stringContains rendered "data-docs-deprecated=\"true\"" "deprecation metadata"
            Expect.stringContains rendered "datetime=\"2026-08-12\"" "last-updated metadata"
            Expect.stringContains rendered "href=\"https://github.com/example/docs/edit/main/guide.fs\"" "edit source"
        }

        test "Document builders render accessible navigation and arbitrary content" {
            let page =
                DocumentationPage.create "detail" "Detail" |> DocumentationPage.withDescription "A customizable page." |> DocumentationPage.withLayout Canvas |> DocumentationPage.withRightRail NoRail |> DocumentationPage.withHiddenHeading |> DocumentationPage.withSections [
                    DocumentationSection.create "diagram" "Diagram" [ sequenceDiagram () |> SequenceDiagram.render |> Mermaid.create |> Mermaid.render ]
                    DocumentationSection.create "custom" "Custom" [ div { _data("example", "true"); "Product content" } ] ]

            let breadcrumbs = Navigation.breadcrumbs navigation site.homeId page.activeId
            let rendered =
                Document.create site page
                |> Document.withBreadcrumbs breadcrumbs
                |> Document.withSideNavItems navigation
                |> Document.render
                |> Render.toString
            let manifest = docsTailwindManifest ()
            Expect.throws (fun () -> Document.create site page |> Document.withBreadcrumbs [] |> ignore) "explicit breadcrumb configuration cannot be empty"
            Expect.throws (fun () -> Document.create site page |> Document.withSideNavItems [] |> ignore) "explicit side navigation cannot be empty"
            Expect.isFalse (rendered.Contains("<style")) "component presentation is consumer-compiled"
            Expect.stringContains rendered "rel=\"stylesheet\" href=\"/css/compiled.css\"" "default compiled stylesheet contract"
            Expect.stringContains rendered "class=\"spec-heading-visually-hidden\"" "hidden semantic heading"
            Expect.stringContains rendered "aria-label=\"Toggle Guides section\"" "accessible disclosure"
            Expect.stringContains rendered "class=\"spec-nav-chevron\"" "groups expose compact disclosure chevrons"
            Expect.stringContains rendered "class=\"spec-nav-chevron-spacer\" aria-hidden=\"true\"" "pages reserve the disclosure column"
            Expect.stringContains rendered "href=\"/guides\"" "breadcrumb destination"
            Expect.stringContains rendered "class=\"mermaid spec-diagram\"" "sequence diagram component"
            Expect.stringContains rendered "data-init=\"window.renderMermaid?.(el)\"" "diagrams initialize when Datastar adds them to the DOM"
            Expect.stringContains rendered "data-mermaid-source=\"sequenceDiagram" "diagram source is encoded outside visible content"
            Expect.stringContains rendered "data-mermaid-state=\"pending\" aria-busy=\"true\"" "diagrams expose their initial busy state"
            Expect.stringContains rendered "data-mermaid-status=\"true\" role=\"status\">Rendering diagram…</p>" "diagrams provide accessible pending content"
            Expect.stringContains rendered "data-example=\"true\"" "custom product content"
            Expect.stringContains rendered "https://github.com/example/docs" "optional repository link"
            Expect.stringContains rendered "aria-label=\"View repository on GitHub\"" "GitHub repository action is icon-only and accessibly named"
            Expect.stringContains rendered "id=\"spec-color-mode-trigger\"" "built-in color mode selector"
            Expect.stringContains rendered "aria-haspopup=\"menu\"" "color mode uses the shared DropdownMenu"
            Expect.stringContains rendered "role=\"menuitemradio\"" "color mode options use radio menu semantics"
            Expect.stringContains rendered "window.fsharpDocsColorMode" "color mode is applied before paint and persisted"
            Expect.stringContains rendered "window.fsharpDocsPreviewColorMode" "isolated documentation previews inherit the resolved host color mode"
            Expect.stringContains rendered "iframe[data-docs-preview-src]" "color-mode synchronization is bounded to documentation preview frames"
            Expect.stringContains manifest "--docs-code-bg: #f6f8fa" "light mode uses a light code surface"
            Expect.stringContains manifest "--docs-code-bg: #0d1117" "dark mode uses a dark code surface"
            Expect.stringContains manifest ".spec-document .token.atrule" "Prism tokens follow the active color mode"
            Expect.stringContains manifest ".spec-document pre.spec-code code" "package code selector overrides host styles"
            Expect.stringContains manifest "--docs-text-code: 0.875rem" "code uses the semantic 14px role"
            Expect.stringContains manifest "font-size: var(--docs-text-code)" "code selectors use semantic compact typography"
            Expect.stringContains rendered "rel=\"canonical\" href=\"https://docs.example.com/guides/detail\"" "canonical page URL"
            Expect.stringContains rendered "name=\"description\" content=\"A customizable page.\"" "page-specific description"
        }

        test "Repository links and default color modes remain consumer configurable" {
            let configuredSite =
                { site with
                    repository = Some(DocsRepository.link "Source repository" "https://code.example.com/project")
                    defaultColorMode = DocsColorMode.Dark }

            let rendered = DocumentationPage.create "guide" "Guide" |> DocumentationPage.withDescription "Description" |> DocumentationPage.withSections [] |> fun page -> Document.create configuredSite page |> Document.render |> Render.toString
            Expect.stringContains rendered "href=\"https://code.example.com/project\"" "custom repository URL"
            Expect.stringContains rendered ">Source repository</a>" "custom repository label"
            Expect.stringContains rendered "defaultMode: \"dark\"" "consumer default is serialized before paint"
            Expect.stringContains rendered "id=\"spec-color-mode-menu-entry-2\" type=\"button\" role=\"menuitemradio\" aria-checked=\"true\"" "the default Dark radio item is checked before hydration"
        }

        test "Rich API operations cover authentication, located parameters, responses, errors, and policy metadata" {
            let operation =
                Operation.create POST "/v1/items/{id}"
                |> Operation.withDescription "Update an item"
                |> Operation.withAuthentication "Bearer token"
                |> Operation.withParameters [
                    Parameter.create "id" "string" Path
                    |> Parameter.required
                    |> Parameter.withDescription "Item identifier."
                    |> Parameter.withExample "item_123"
                    Parameter.create "mode" "string" Query
                    |> Parameter.withDescription "Update mode."
                    |> Parameter.withDefaultValue "safe"
                    |> Parameter.withEnumValues [ "safe"; "force" ] ]
                |> Operation.withResponses [
                    Response.create "200"
                    |> Response.withDescription "Updated"
                    |> Response.withExample "json" "{ \"id\": \"item_123\" }"
                    Response.create "404" |> Response.withDescription "Not found" ]
                |> Operation.withErrors [ Error.create "item_not_found" "The item does not exist." ]
                |> Operation.withIdempotency "Requests are idempotent for 24 hours."
                |> Operation.withApiVersion "2026-08-01"
                |> Operation.deprecated
                |> Operation.render
                |> Render.toString

            Expect.stringContains operation "Bearer token" "authentication"
            Expect.stringContains operation "data-parameter-location=\"path\"" "parameter location"
            Expect.stringContains operation "safe, force" "enum values"
            Expect.stringContains operation "item_not_found" "error model"
            Expect.stringContains operation "Idempotency" "retry/idempotency guidance"
            Expect.stringContains operation "2026-08-01" "API version"
            Expect.stringContains operation "Deprecated" "deprecation badge"
        }

        test "Reference builders render endpoint, parameters, request, and response examples" {
            let endpoint =
                Endpoint.create POST "/v1/items"
                |> Endpoint.withDescription "Creates an item."
                |> Endpoint.render
                |> Render.toString
            let parameters =
                ApiReference.parameters [
                    ApiReference.parameter "name" "string" true "The display name."
                    ApiReference.parameter "metadata" "object" false "Additional values." ]
                |> Render.toString
            let request = ApiReference.codeExample "Create item" "curl" "curl --request POST https://api.example.com/v1/items" |> Render.toString
            let response = ApiReference.responseExample "201" "json" "{ \"id\": \"item_123\" }" |> Render.toString

            Expect.stringContains endpoint "data-http-method=\"POST\"" "method metadata"
            Expect.stringContains endpoint "/v1/items" "endpoint path"
            Expect.stringContains parameters "name" "parameter name"
            Expect.stringContains parameters "Required" "required marker"
            Expect.stringContains request "Create item" "request example title"
            Expect.stringContains response "201" "response status"
        }

        test "Pages declare optional runtime asset requirements" {
            let plain = DocumentationPage.create "guide" "Guide" |> DocumentationPage.withDescription "Description" |> DocumentationPage.withSections []
            let diagramSource = "flowchart LR\nA[\"<script>alert('diagram')</script>\"] --> B[Ready]"
            let diagram = DocumentationPage.create "guide" "Guide" |> DocumentationPage.withDescription "Description" |> DocumentationPage.withSections [ DocumentationSection.create "flow" "Flow" [ Mermaid.create diagramSource |> Mermaid.render ] ]
            let highlighted = DocumentationPage.create "guide" "Guide" |> DocumentationPage.withDescription "Description" |> DocumentationPage.withSections [ DocumentationSection.create "code" "Code" [ CodeBlock.create "fsharp" "let x = 1" |> CodeBlock.render ] ]

            let plainHtml = Document.create site plain |> Document.render |> Render.toString
            let diagramHtml = Document.create site diagram |> Document.render |> Render.toString
            let highlightedHtml = Document.create site highlighted |> Document.render |> Render.toString
            Expect.stringContains plainHtml "window.fsharpDocsMermaid" "plain pages configure lazy Mermaid for later navigation"
            Expect.stringContains plainHtml "/scripts/mermaid.11.16.0.min.js" "plain pages retain the configured Mermaid source"
            Expect.isFalse (plainHtml.Contains("src=\"/scripts/mermaid.11.16.0.min.js\"")) "plain pages do not eagerly load Mermaid"
            let manifest = docsTailwindManifest ()
            Expect.isFalse (plainHtml.Contains("prism-tomorrow.1.29.0.min.css")) "default Prism colors come from the consumer-compiled manifest rather than a dark-only stylesheet"
            Expect.stringContains manifest "--docs-code-green: #116329" "light Prism palette is available before highlighting"
            Expect.stringContains manifest "--docs-code-green: #7ee787" "dark Prism palette is available before highlighting"
            Expect.isFalse (plainHtml.Contains("src=\"/scripts/prism.1.29.0.min.js\"")) "plain pages omit Prism scripts"
            Expect.stringContains diagramHtml "data-init=\"window.renderMermaid?.(el)\"" "diagram elements own their Datastar initialization"
            Expect.stringContains diagramHtml "data-mermaid-source=\"flowchart LR" "diagram source is encoded as data rather than visible content"
            Expect.stringContains diagramHtml "&lt;script&gt;alert(&#39;diagram&#39;)&lt;/script&gt;" "diagram source cannot escape its encoded data attribute"
            Expect.isFalse (diagramHtml.Contains("<script>alert('diagram')</script>")) "trusted Mermaid source is never emitted as executable component markup"
            Expect.stringContains diagramHtml "role=\"status\">Rendering diagram…</p>" "diagram pages render accessible pending content"
            Expect.isFalse (diagramHtml.Contains(">flowchart LR</div>")) "diagram pages never render Mermaid source as visible content"
            Expect.isFalse (diagramHtml.Contains("src=\"/scripts/mermaid.11.16.0.min.js\"")) "diagram pages also load Mermaid lazily"
            Expect.stringContains highlightedHtml "prism.1.29.0" "code pages configure Prism"
        }

        test "CSP nonces apply to package-owned scripts without embedded presentation" {
            let configuredSite = { site with assets = { DocsAssets.defaults with nonce = Some "request-nonce" } }
            let rendered = DocumentationPage.create "guide" "Guide" |> DocumentationPage.withDescription "Description" |> DocumentationPage.withSections [] |> fun page -> Document.create configuredSite page |> Document.render |> Render.toString

            Expect.isFalse (rendered.Contains("<style")) "the package emits no style element requiring a nonce"
            Expect.stringContains rendered "<script nonce=\"request-nonce\">" "inline script nonce"
            Expect.stringContains rendered "type=\"module\" src=\"/scripts/datastar.1.0.2.js\" nonce=\"request-nonce\"" "external runtime nonce"
        }

        test "Assets and Mermaid behavior are configurable" {
            let assets =
                { DocsAssets.defaults with
                    productStylesheets = [ "/css/product.css" ]
                    prismStylesheet = Some "/css/custom-prism.css"
                    mermaidSecurityLevel = "strict"
                    additionalHead = [ meta { _name "robots"; _content "noindex" } ] }

            let configuredSite = { site with assets = assets }
            let page = DocumentationPage.create "guide" "Guide" |> DocumentationPage.withDescription "Description" |> DocumentationPage.withSections [ DocumentationSection.create "flow" "Flow" [ Mermaid.create "flowchart LR" |> Mermaid.render ] ]
            let rendered = Document.create configuredSite page |> Document.render |> Render.toString

            Expect.stringContains rendered "href=\"/css/product.css\"" "product stylesheet"
            Expect.stringContains rendered "href=\"/css/custom-prism.css\"" "consumer Prism stylesheet overrides the embedded palette"
            Expect.stringContains rendered "securityLevel: \"strict\"" "Mermaid security setting is safely serialized"
            Expect.stringContains rendered "typeof window.mermaid?.initialize === 'function'" "Mermaid API detection cannot be clobbered by a consumer element ID"
            Expect.stringContains rendered "suppressErrorRendering: true" "Mermaid error SVGs are suppressed in favor of package-owned failure content"
            Expect.stringContains rendered "window.mermaid.render(id, node.dataset.mermaidSource" "Mermaid renders from encoded source without restoring visible raw text"
            Expect.stringContains rendered "mermaidRenderQueue" "Mermaid renders are serialized"
            Expect.stringContains rendered "window.renderMermaid?.(content, true)" "initial document readiness does not rely solely on per-element Datastar initialization"
            Expect.stringContains rendered "setMermaidFailed" "asset and render failures use the shared deterministic failure state"
            Expect.stringContains rendered "name=\"robots\"" "additional head content"
        }

        test "Code blocks and API examples provide accessible copy controls" {
            let section =
                DocumentationSection.create
                    "code"
                    "Code"
                    [ CodeBlock.create "fsharp" "let value = 42" |> CodeBlock.render ]
            let page =
                DocumentationPage.create "guide" "Guide"
                |> DocumentationPage.withDescription "Description"
                |> DocumentationPage.withSections [ section ]
            let document = Document.create site page |> Document.render |> Render.toString
            let api = ApiReference.codeExample "Request" "curl" "curl https://example.com" |> Render.toString

            Expect.stringContains document "aria-label=\"Copy code\"" "standard code copy button"
            Expect.stringContains document "data-docs-copy-source" "standard source relationship"
            Expect.stringContains api "aria-label=\"Copy Request\"" "API code copy button"
            Expect.stringContains document "window.fsharpDocsCopy" "document copy lifecycle"
            let example = Example.codeFirst "copy-example" "Example" "fsharp" "let value = 42" (div { "42" }) |> Render.toString
            for html in [ document; api; example ] do
                for state in [ "copy"; "success"; "error" ] do
                    Expect.stringContains html ($"data-docs-copy-icon=\"{state}\"") "all code surfaces share icon states"
                Expect.stringContains html "class=\"sr-only\" role=\"status\"" "copy feedback is announced without visible button text"
                Expect.isFalse (html.Contains(">Copy</span>")) "copy controls are icon-only"
            Expect.throws (fun () -> CodeBlock.create "fsharp" "" |> ignore) "code source is required"
        }

        test "Callout and Mermaid components retain their package-owned presentation" {
            let content = p { "Render trusted content only." }
            let callout =
                Callout.create "Security" [ content ]
                |> Callout.render
                |> Render.toString
            let c4 =
                Mermaid.create "C4Context\ntitle Documentation"
                |> Mermaid.withC4
                |> Mermaid.render
                |> Render.toString

            Expect.stringContains callout "class=\"spec-callout-label\">Security" "callout label"
            Expect.stringContains callout "Render trusted content only." "semantic callout content"
            Expect.stringContains c4 "spec-c4-diagram" "C4 diagram surface"
            Expect.throws (fun () -> Mermaid.create "" |> ignore) "diagram source is required"
            Expect.throws (fun () -> Callout.create "" [] |> ignore) "callout label is required"
        }

        test "Gallery pages keep named examples without duplicate headings or article rails" {
            let page =
                DocumentationPage.create "buttons" "Buttons" |> DocumentationPage.withLayout Gallery |> DocumentationPage.withRightRail NoRail |> DocumentationPage.withSections [
                    DocumentationSection.create "primary" "Primary" [
                        Example.gallery "primary-button" "Primary" "fsharp" "Button.primary \"Create\"" (button { _type "button"; "Create" }) ] ]
            let html = Document.create site page |> Document.render |> Render.toString
            Expect.equal page.layout Gallery "explicit gallery layout"
            Expect.stringContains html "docs-gallery-layout" "gallery layout hook"
            Expect.isFalse (html.Contains("class=\"spec-toc-nav")) "no article table of contents"
            Expect.stringContains html "<section id=\"primary\" tabindex=\"-1\">" "stable section fragment"
            Expect.stringContains html "<h2 class=\"spec-example-title\">Primary</h2>" "example toolbar owns its heading"
            Expect.isFalse (html.Contains("spec-section-title-level-2")) "section does not repeat the example heading"
            Expect.stringContains html "aria-label=\"Copy Primary code\"" "copy action has an example-specific name"
            Expect.stringContains html "data-docs-copy-source=\"true\"" "copy targets the literal source"
            Expect.isLessThan (html.IndexOf(">Preview</button>")) (html.IndexOf(">Code</button>")) "preview is the first tab"
        }

        test "Examples provide accessible independent preview and code tabs" {
            let example =
                Example.codeFirst "counter-example" "Counter" "fsharp" "button { \"Increment\" }" (button { _type "button"; "Increment" })
                |> Render.toString

            Expect.stringContains example "data-docs-example=\"true\"" "example marker"
            Expect.stringContains example "role=\"tablist\" aria-label=\"Counter\"" "accessible tab list"
            Expect.stringContains example "role=\"tab\"" "tab semantics"
            Expect.stringContains example "aria-controls=\"counter-example-panel-code\"" "code panel relationship"
            Expect.stringContains example "class=\"spec-example-preview\"" "consumer preview"
            Expect.stringContains example "class=\"spec-example-code spec-code language-fsharp\"" "Prism-compatible code"
            Expect.stringContains example "counter_exampleExample: &#39;code&#39;" "developer examples show source first"
            Expect.isLessThan
                (example.IndexOf(">Code</button>", System.StringComparison.Ordinal))
                (example.IndexOf(">Preview</button>", System.StringComparison.Ordinal))
                "code appears before preview in the toggle"
            Expect.stringContains example "window.renderCode" "opening code highlights dynamic content"
            Expect.stringContains example "window.renderDocsPreview" "opening preview initializes isolated pages and dynamic diagrams"
            Expect.stringContains example "aria-label=\"Copy Counter code\"" "every example exposes a direct source copy action"
            Expect.stringContains example "data-docs-copy-source=\"true\"" "every example identifies the exact source to copy"

            let previewFirst =
                Example.previewFirst "preview-example" "Preview" "fsharp" "div { \"Source\" }" (div { "Preview" })
                |> Render.toString
            Expect.stringContains previewFirst "preview_exampleExample: &#39;preview&#39;" "preview-first examples initialize independently"
            Expect.stringContains previewFirst "data-docs-preview-initial=\"true\"" "initial previews opt into lifecycle initialization"

            let codeFirst =
                Example.codeFirst "source-example" "Source" "fsharp" "div { \"Source\" }" (div { "Preview" })
                |> Render.toString
            Expect.stringContains codeFirst "source_exampleExample: &#39;code&#39;" "code-first examples initialize independently"
        }

        test "Version selectors are opt-in navigation" {
            let versions = VersionView.selector "2026.8" [ DocsVersion.create "2026.8" "/"; DocsVersion.create "2026.2" "/v2026.2" ] |> Render.toString

            Expect.stringContains versions "aria-label=\"Documentation version\"" "version selector label"
            Expect.stringContains versions "aria-current=\"page\"" "current version"
        }

        test "Browser frames and tabs are reusable independent components" {
            let framed =
                Browser.create (div { "Application" })
                |> Browser.withAddress "https://example.com/items"
                |> Browser.render
                |> Render.toString
            Expect.stringContains framed "data-browser-frame=\"true\"" "browser marker"
            Expect.stringContains framed "data-browser-url=\"https://example.com/items\"" "canonical URL marker"

            let tabs =
                [ Tab.create "empty" "Empty" (div { "No items" })
                  Tab.create "ready" "Ready" (div { "Items" }) ]
                |> Tabs.create "item-states" "Item states"
                |> Tabs.render
                |> Render.toString

            Expect.stringContains tabs "role=\"tablist\"" "tab list semantics"
            Expect.stringContains tabs "aria-controls=\"item-states-panel-" "tab controls panel"
            Expect.stringContains tabs "role=\"tabpanel\"" "tab panel semantics"
        }

        test "App mode keeps workflow destinations and review states explicit" {
            let ready = FixtureState.create "Ready" "/checkout/shipping" |> FixtureState.current
            let invalid = FixtureState.create "Address error" "/checkout/shipping?state=error"
            let frame =
                Browser.create (div { "Shipping address" })
                |> Browser.withAddress "https://shop.example.test/checkout/shipping"
                |> Browser.withAppMode "checkout-shipping" "Shipping address"
                |> Browser.render
                |> Fixture.create "checkout-shipping"
                |> Fixture.withPrevious (FixtureLink.create "Cart" "/checkout/cart")
                |> Fixture.withNext (FixtureLink.create "Payment" "/checkout/payment")
                |> Fixture.withStates [ ready; invalid ]
                |> Fixture.render
                |> Render.toString

            Expect.stringContains frame "data-fve-app-mode-frame-id=\"checkout-shipping\"" "stable frame identity"
            Expect.stringContains frame "data-fve-app-mode-previous=\"true\"" "previous workflow destination"
            Expect.stringContains frame "data-fve-app-mode-next=\"true\"" "next workflow destination"
            Expect.stringContains frame "data-fve-app-mode-state-select=\"true\"" "shared review-state select"
            Expect.stringContains frame "role=\"combobox\"" "shared Select interaction"
            Expect.stringContains frame "data-fve-app-mode-state=\"true\"" "review state destination"
            Expect.stringContains frame "aria-current=\"page\"" "current review state"

            let phone =
                Phone.create (div { "Phone content" })
                |> Phone.withAppMode "checkout-phone" "Shipping address on phone"
                |> Phone.render
                |> Render.toString

            Expect.stringContains phone "data-fve-phone=\"true\"" "standalone phone primitive"
            Expect.stringContains phone "data-fve-app-mode-frame-id=\"checkout-phone\"" "phone owns its App-mode identity"
        }

        test "Graph validation is available without prescribing architecture depth" {
            let graph =
                { nodes = [ Home; Guide; Detail ]
                  roots = [ Home ]
                  edges = [ Home, Guide; Guide, Detail; Detail, Reference ] }

            let codes = DirectedGraph.validate graph |> issueCodes
            Expect.isTrue (Set.contains "graph.unknown-target" codes) "unknown target"
            Expect.isFalse (Set.contains "graph.unreachable" codes) "all declared nodes are reachable"
        }
    ]
