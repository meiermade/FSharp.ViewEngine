module DocsTests

open Expecto
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

type Destination =
    | Home
    | Guide
    | Detail
    | Reference

let private issueCodes (issues:ValidationIssue list) = issues |> List.map _.code |> Set.ofList

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
      baseUrl = Some "https://fve.meiermade.com"
      description = Some "Example product documentation."
      repository = Some(DocsRepository.github "https://github.com/meiermade/FSharp.ViewEngine")
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

        test "External navigation is not a registered page and opens in a new tab" {
            let link = Nav.externalLink "releases" "Releases" "https://github.com/meiermade/FSharp.ViewEngine/releases"
            let nodes = navigation @ [link]
            Expect.isEmpty (Navigation.validate nodes) "external HTTPS navigation is valid"
            Expect.equal (NavNode.collectPages nodes |> List.length) 4 "external links do not enter the page registry"
            Expect.isNone (NavNode.destination link) "no typed application destination"
            let page = DocumentationPage.create "home" "Home"
            let html = DocsView.document { site with navigation = nodes } page |> Render.toString
            Expect.stringContains html "target=\"_blank\" rel=\"noopener\" aria-describedby=\"nav-releases-hint\"" "safe external tab with accessible hint"
            Expect.stringContains html "id=\"nav-releases\"" "stable navigation identity"
            Expect.isFalse (html.Contains("id=\"nav-releases\" data-docs-nav-link")) "external links are not enhanced application navigation"
            let invalid = Nav.externalLink "unsafe" "Unsafe" "javascript:alert(1)"
            Expect.isNonEmpty (Navigation.validate [invalid]) "executable URLs are rejected"
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
                        p { "Read "; strong { "carefully" }; " and "; a { _href "/next"; "continue" }; " with "; code { "DocumentationPage.create" } }
                        ul { li { "Plain list item" } }
                        ol { li { "Ordered item" } }
                        div { table { thead { tr { th { "Builder" }; th { "Purpose" } } }; tbody { tr { td { code { "DocumentationPage.create" } }; td { "Guides" } } } } }
                        fveCallout { _label "Note"; p { "Direct typed HTML stays semantic." } } ] ]

            let rendered = DocumentationPage.render page |> Render.toString
            Expect.stringContains rendered "<strong>carefully</strong>" "strong inline content"
            Expect.stringContains rendered "href=\"/next\"" "inline link"
            Expect.stringContains rendered "<code>DocumentationPage.create</code>" "inline code"
            Expect.stringContains rendered "<ul><li>Plain list item</li></ul>" "plain semantic list"
            Expect.stringContains rendered "<ol><li>Ordered item</li></ol>" "ordered semantic list"
            Expect.stringContains rendered "<th>Builder</th>" "semantic table header"
            Expect.stringContains rendered "data-docs-callout=\"true\"" "callout hook"
            Expect.stringContains rendered ">Note</div>" "callout label"
        }

        test "Heading adornments and lead content preserve the semantic page heading" {
            let page =
                DocumentationPage.create "home" "Home" |> DocumentationPage.withDescription "Description"
                |> DocumentationPage.withHeadingAdornment (img { _src "/logo.svg"; _alt "" })
                |> DocumentationPage.withLead [ div { _data("docs-lead-example", "true"); "Lead example" } ]
                |> DocumentationPage.withSections [ DocumentationSection.create "usage" "Usage" [ p { "Use it." } ] ]
            let rendered = DocumentationPage.render page |> Render.toString

            Expect.stringContains rendered "src=\"/logo.svg\"" "adornment is rendered before the title"
            Expect.equal (rendered.Split("<h1").Length - 1) 1 "page retains one semantic h1"
            Expect.isLessThan (rendered.IndexOf("<h1", System.StringComparison.Ordinal)) (rendered.IndexOf("data-docs-lead-example", System.StringComparison.Ordinal)) "the lead follows the page heading"
            Expect.isLessThan (rendered.IndexOf("data-docs-lead-example", System.StringComparison.Ordinal)) (rendered.IndexOf("id=\"usage\"", System.StringComparison.Ordinal)) "the lead precedes outlined sections"
        }

        test "Article, reference, and canvas builders select composable layouts" {
            let sections = [ DocumentationSection.create "usage" "Usage" [ fveCodeBlock { yield _language ("shell"); yield ("example run" : string) }; p { "Any page shape is allowed." } ] ]
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
            Expect.isEmpty (DocumentationPage.validate reference) "arbitrary valid sections pass structural validation"
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
            Expect.isFalse (rendered.Contains("window.fsharpDocsNavigation")) "the reusable pager remains independent of HTTP navigation transport"
        }

        test "Search indexes include page titles, headings, descriptions, and consumer keywords" {
            let page = DocumentationPage.create "guide" "Build a view" |> DocumentationPage.withDescription "Compose typed HTML." |> DocumentationPage.withSections [ DocumentationSection.create "render" "Render output" [] ]
            let entry = DocsSearchEntry.create "/guide" page [ "serialization" ]
            let index = DocsSearch.index [ entry ]
            let rendered = SearchView.render index |> Render.toString

            Expect.sequenceEqual index[0].keywords [ "serialization" ] "page keywords do not inherit every section topic"
            Expect.stringContains rendered "aria-label=\"Search documentation\"" "search dialog label"
            Expect.stringContains rendered "data-fve-command-item" "shared command result metadata"
            Expect.equal index[1].description "" "section results do not repeat page-wide description matches"
            Expect.equal index[0].group "Pages" "ordinary page results have a useful default group"
            Expect.equal index[1].group "Sections" "deep links are secondary to all page groups"
            Expect.equal index[1].sectionTitle (Some "Render output") "section matching has an explicit anchor"
            Expect.stringContains rendered "data-fve-command-match-anchor=\"Render output\"" "deep links require a relevant heading match"
            let example = DocsSearchEntry.create "/examples/presentation" page [] |> DocsSearchEntry.withGroup "Examples" |> DocsSearchEntry.inNewTab
            let exampleHtml = DocsSearch.index [example] |> SearchView.render |> Render.toString
            Expect.stringContains exampleHtml "target=\"_blank\"" "examples retain native new-tab destinations"
            Expect.stringContains exampleHtml "rel=\"noopener\"" "new-tab results do not retain an opener"
            Expect.stringContains rendered "href=\"/guide#render\"" "heading deep link"
            Expect.stringContains rendered "Ctrl+K" "keyboard shortcut hint"
        }

        test "Page metadata controls browser, search, and social metadata" {
            let page =
                DocumentationPage.create "guide" "Guide" |> DocumentationPage.withDescription "A focused guide description." |> DocumentationPage.withSections []
                |> DocumentationPage.withMetadata {
                    DocsPageMetadata.defaults with
                        browserTitle = Some "Guide · Example"
                        canonicalUrl = Some "https://fve.meiermade.com/docs"
                        noIndex = true
                        socialImage = Some "https://fve.meiermade.com/favicon.svg"
                        version = Some "2026.8"
                        deprecated = true
                        lastUpdated = Some "2026-08-12"
                        editUrl = Some "https://github.com/meiermade/FSharp.ViewEngine/edit/main/README.md" }

            let rendered = Document.create site page |> Document.render |> Render.toString
            Expect.stringContains rendered "<title id=\"docs-document-title\">Guide &#183; Example</title>" "browser title"
            Expect.stringContains rendered "name=\"description\" content=\"A focused guide description.\"" "page description"
            Expect.stringContains rendered "rel=\"canonical\" href=\"https://fve.meiermade.com/docs\"" "canonical override"
            Expect.stringContains rendered "name=\"robots\" content=\"noindex\"" "robots metadata"
            Expect.stringContains rendered "property=\"og:url\" content=\"https://fve.meiermade.com/docs\"" "Open Graph canonical URL"
            Expect.stringContains rendered "property=\"og:type\" content=\"website\"" "Open Graph type"
            Expect.stringContains rendered "property=\"og:site_name\" content=\"Example\"" "Open Graph site name"
            Expect.stringContains rendered "property=\"og:image\" content=\"https://fve.meiermade.com/favicon.svg\"" "social image"
            Expect.stringContains rendered "property=\"og:image:alt\" content=\"Guide\"" "social image alternative"
            Expect.stringContains rendered "data-docs-version=\"2026.8\"" "version metadata"
            Expect.stringContains rendered "data-docs-deprecated=\"true\"" "deprecation metadata"
            Expect.stringContains rendered "datetime=\"2026-08-12\"" "last-updated metadata"
            Expect.stringContains rendered "href=\"https://github.com/meiermade/FSharp.ViewEngine/edit/main/README.md\"" "edit source"
        }

        test "Document builders render accessible navigation and arbitrary content" {
            let page =
                DocumentationPage.create "detail" "Detail" |> DocumentationPage.withDescription "A customizable page." |> DocumentationPage.withLayout Canvas |> DocumentationPage.withRightRail NoRail |> DocumentationPage.withHiddenHeading |> DocumentationPage.withSections [
                    DocumentationSection.create "diagram" "Diagram" [ fveMermaid { SequenceDiagram.render (sequenceDiagram ()) } ]
                    DocumentationSection.create "custom" "Custom" [ div { _data("example", "true"); "Product content" } ] ]

            let breadcrumbs = Navigation.breadcrumbs navigation site.homeId page.activeId
            let rendered =
                Document.create site page
                |> Document.withBreadcrumbs breadcrumbs
                |> Document.withSideNavItems navigation
                |> Document.render
                |> Render.toString
            Expect.throws (fun () -> Document.create site page |> Document.withBreadcrumbs [] |> ignore) "explicit breadcrumb configuration cannot be empty"
            Expect.throws (fun () -> Document.create site page |> Document.withSideNavItems [] |> ignore) "explicit side navigation cannot be empty"
            Expect.isFalse (rendered.Contains("<style")) "component presentation is consumer-compiled"
            Expect.stringContains rendered "rel=\"stylesheet\" href=\"/css/compiled.css\"" "default compiled stylesheet contract"
            Expect.stringContains rendered "data-docs-visually-hidden-heading=\"true\"" "hidden semantic heading"
            Expect.stringContains rendered "aria-label=\"Toggle Guides section\"" "accessible disclosure"
            Expect.stringContains rendered "<details" "groups preserve native disclosure state"
            Expect.stringContains rendered "aria-expanded=" "groups expose accessible disclosure state"
            Expect.stringContains rendered "href=\"/guides\"" "breadcrumb destination"
            Expect.stringContains rendered "class=\"block mermaid overflow-x-auto" "sequence diagram component"
            Expect.stringContains rendered "<fve-mermaid " "diagrams keep an instance-local lifecycle owner"
            Expect.stringContains rendered "data-mermaid-source=\"sequenceDiagram" "diagram source is encoded outside visible content"
            Expect.stringContains rendered "data-mermaid-state=\"pending\" aria-busy=\"true\"" "diagrams expose their initial busy state"
            Expect.stringContains rendered "data-mermaid-status=\"true\" role=\"status\">Rendering diagram…</p>" "diagrams provide accessible pending content"
            Expect.stringContains rendered "data-example=\"true\"" "custom product content"
            Expect.stringContains rendered "https://github.com/meiermade/FSharp.ViewEngine" "optional repository link"
            Expect.stringContains rendered "aria-label=\"View repository on GitHub\"" "GitHub repository action is icon-only and accessibly named"
            Expect.stringContains rendered "id=\"docs-color-mode-trigger\"" "built-in color mode selector"
            Expect.stringContains rendered "aria-haspopup=\"menu\"" "color mode uses the shared DropdownMenu"
            Expect.stringContains rendered "role=\"menuitemradio\"" "color mode options use radio menu semantics"
            Expect.stringContains rendered "window.fveColorMode" "shared color mode is applied before paint and persisted"
            Expect.stringContains rendered "window.fsharpDocsPreviewColorMode" "isolated documentation previews inherit the resolved host color mode"
            Expect.stringContains rendered "iframe[data-docs-preview-src]" "color-mode synchronization is bounded to documentation preview frames"
            Expect.stringContains rendered "rel=\"canonical\" href=\"https://fve.meiermade.com/guides/detail\"" "canonical page URL"
            Expect.stringContains rendered "name=\"description\" content=\"A customizable page.\"" "page-specific description"
        }

        test "Repository links and default color modes remain consumer configurable" {
            let configuredSite =
                { site with
                    repository = Some(DocsRepository.link "Source repository" "https://github.com/meiermade/FSharp.ViewEngine")
                    defaultColorMode = DocsColorMode.Dark }

            let rendered = DocumentationPage.create "guide" "Guide" |> DocumentationPage.withDescription "Description" |> DocumentationPage.withSections [] |> fun page -> Document.create configuredSite page |> Document.render |> Render.toString
            Expect.stringContains rendered "href=\"https://github.com/meiermade/FSharp.ViewEngine\"" "custom repository URL"
            Expect.stringContains rendered ">Source repository</a>" "custom repository label"
            Expect.stringContains rendered "defaultMode: \"dark\"" "consumer default is serialized before paint"
            Expect.stringContains rendered "id=\"docs-color-mode-menu-entry-2\" type=\"button\" role=\"menuitemradio\" aria-checked=\"true\"" "the default Dark radio item is checked before hydration"
        }

        test "Rich API operations cover authentication, located parameters, responses, errors, and policy metadata" {
            let operation =
                ApiOperation.create POST "/v1/items/{id}"
                |> ApiOperation.withDescription "Update an item"
                |> ApiOperation.withAuthentication "Bearer token"
                |> ApiOperation.withParameters [
                    ApiParameter.create "id" "string" Path
                    |> ApiParameter.required
                    |> ApiParameter.withDescription "Item identifier."
                    |> ApiParameter.withExample "item_123"
                    ApiParameter.create "mode" "string" Query
                    |> ApiParameter.withDescription "Update mode."
                    |> ApiParameter.withDefaultValue "safe"
                    |> ApiParameter.withEnumValues [ "safe"; "force" ] ]
                |> ApiOperation.withResponses [
                    ApiResponse.create "200"
                    |> ApiResponse.withDescription "Updated"
                    |> ApiResponse.withExample "json" "{ \"id\": \"item_123\" }"
                    ApiResponse.create "404" |> ApiResponse.withDescription "Not found" ]
                |> ApiOperation.withErrors [ ApiError.create "item_not_found" "The item does not exist." ]
                |> ApiOperation.withIdempotency "Requests are idempotent for 24 hours."
                |> ApiOperation.withApiVersion "2026-08-01"
                |> ApiOperation.deprecated
                |> ApiOperation.render
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
                ApiEndpoint.create POST "/v1/items"
                |> ApiEndpoint.withDescription "Creates an item."
                |> ApiEndpoint.render
                |> Render.toString
            let parameters =
                ApiReference.parameters [
                    ApiReference.parameter "name" "string" true "The display name."
                    ApiReference.parameter "metadata" "object" false "Additional values." ]
                |> Render.toString
            let request = ApiReference.codeExample "Create item" "curl" "curl --request POST $API_ORIGIN/v1/items" |> Render.toString
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
            let diagram = DocumentationPage.create "guide" "Guide" |> DocumentationPage.withDescription "Description" |> DocumentationPage.withSections [ DocumentationSection.create "flow" "Flow" [ fveMermaid { yield (diagramSource : string) } ] ]
            let highlighted = DocumentationPage.create "guide" "Guide" |> DocumentationPage.withDescription "Description" |> DocumentationPage.withSections [ DocumentationSection.create "code" "Code" [ fveCodeBlock { yield _language ("fsharp"); yield ("let x = 1" : string) } ] ]

            let plainHtml = Document.create site plain |> Document.render |> Render.toString
            let diagramHtml = Document.create site diagram |> Document.render |> Render.toString
            let highlightedHtml = Document.create site highlighted |> Document.render |> Render.toString
            Expect.stringContains plainHtml "window.fsharpDocsMermaid" "plain pages configure lazy Mermaid for later navigation"
            Expect.stringContains plainHtml "/scripts/mermaid.11.16.0.min.js" "plain pages retain the configured Mermaid source"
            Expect.isFalse (plainHtml.Contains("src=\"/scripts/mermaid.11.16.0.min.js\"")) "plain pages do not eagerly load Mermaid"
            Expect.isFalse (plainHtml.Contains("prism-tomorrow.1.29.0.min.css")) "consumers opt into a Prism stylesheet through DocsAssets"
            Expect.isFalse (plainHtml.Contains("src=\"/scripts/prism.1.29.0.min.js\"")) "plain pages omit Prism scripts"
            Expect.stringContains diagramHtml "<fve-mermaid " "diagram elements own their initialization"
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
            Expect.stringContains rendered "type=\"module\" src=\"/assets/app.js\" nonce=\"request-nonce\"" "the shared component bundle carries the request nonce"
            Expect.isFalse (rendered.Contains "src=\"/scripts/datastar.1.0.4.js\"") "ordinary Datastar is not loaded alongside Rocket"
        }

        test "Assets and Mermaid behavior are configurable" {
            let assets =
                { DocsAssets.defaults with
                    productStylesheets = [ "/css/product.css" ]
                    prismStylesheet = Some "/css/custom-prism.css"
                    mermaidSecurityLevel = "strict"
                    additionalHead = [ meta { _name "robots"; _content "noindex" } ] }

            let configuredSite = { site with assets = assets }
            let page = DocumentationPage.create "guide" "Guide" |> DocumentationPage.withDescription "Description" |> DocumentationPage.withSections [ DocumentationSection.create "flow" "Flow" [ fveMermaid { yield ("flowchart LR" : string) } ] ]
            let rendered = Document.create configuredSite page |> Document.render |> Render.toString

            Expect.stringContains rendered "href=\"/css/product.css\"" "product stylesheet"
            Expect.stringContains rendered "href=\"/css/custom-prism.css\"" "consumer Prism stylesheet overrides the embedded palette"
            Expect.stringContains rendered "window.fsharpDocsMermaid.security = \"strict\"" "Mermaid security setting is safely serialized"
            Expect.stringContains rendered "name=\"robots\"" "additional head content"
        }

        test "Code blocks and API examples provide accessible copy controls" {
            let section =
                DocumentationSection.create
                    "code"
                    "Code"
                    [ fveCodeBlock { yield _language ("fsharp"); yield ("let value = 42" : string) } ]
            let page =
                DocumentationPage.create "guide" "Guide"
                |> DocumentationPage.withDescription "Description"
                |> DocumentationPage.withSections [ section ]
            let document = Document.create site page |> Document.render |> Render.toString
            let api = ApiReference.codeExample "Request" "curl" "curl $API_ORIGIN" |> Render.toString

            Expect.stringContains document "aria-label=\"Copy code\"" "standard code copy button"
            Expect.stringContains document "data-docs-copy-source" "standard source relationship"
            Expect.stringContains api "aria-label=\"Copy Request\"" "API code copy button"
            Expect.stringContains document "data-docs-copy-button" "copy controls remain native and explicitly owned"
            let example = fveExample { yield _name ("copy-example"); yield _label ("Example"); yield _variant ExampleVariant.CodeFirst; yield _language ("fsharp"); yield fveExampleCode { yield ("let value = 42" : string) }; yield ((div { "42" }) : HtmlElement) } |> Render.toString
            for html in [ document; api; example ] do
                for state in [ "copy"; "success"; "error" ] do
                    Expect.stringContains html ($"data-docs-copy-icon=\"{state}\"") "all code surfaces share icon states"
                Expect.stringContains html "class=\"sr-only\" role=\"status\"" "copy feedback is announced without visible button text"
                Expect.isFalse (html.Contains(">Copy</span>")) "copy controls are icon-only"
            Expect.throws (fun () -> fveCodeBlock { yield _language ("fsharp"); yield ("" : string) } |> ignore) "code source is required"
        }

        test "Callout and Mermaid components retain their package-owned presentation" {
            let content = p { "Render trusted content only." }
            let callout =
                fveCallout { _label "Security"; content }
                |> Render.toString
            let c4 =
                fveMermaid { yield _c4 true; yield ("C4Context\ntitle Documentation" : string) }
                |> Render.toString

            Expect.stringContains callout "data-docs-callout=\"true\"" "callout hook"
            Expect.stringContains callout ">Security</div>" "callout label"
            Expect.stringContains callout "Render trusted content only." "semantic callout content"
            Expect.stringContains c4 "max-sm:[&amp;&gt;svg]:min-w-3xl" "C4 diagram surface"
            Expect.throws (fun () -> fveMermaid { yield ("" : string) } |> ignore) "diagram source is required"
            Expect.throws (fun () -> fveCallout { _label "" } |> ignore) "callout label is required"
        }

        test "Gallery pages keep named examples without duplicate headings or article rails" {
            let page =
                DocumentationPage.create "buttons" "Buttons" |> DocumentationPage.withLayout Gallery |> DocumentationPage.withRightRail NoRail |> DocumentationPage.withSections [
                    DocumentationSection.create "primary" "Primary" [
                        fveExample { yield _name ("primary-button"); yield _label ("Primary"); yield _variant ExampleVariant.Gallery; yield _language ("fsharp"); yield fveExampleCode { yield ("fveButton { \"Create\" }" : string) }; yield ((button { _type "button"; "Create" }) : HtmlElement) } ] ]
            let html = Document.create site page |> Document.render |> Render.toString
            Expect.equal page.layout Gallery "explicit gallery layout"
            Expect.stringContains html "data-docs-layout=\"gallery\"" "gallery layout hook"
            Expect.isFalse (html.Contains("data-docs-toc=\"true\"")) "no article table of contents"
            Expect.stringContains html "<section id=\"primary\" tabindex=\"-1\">" "stable section fragment"
            Expect.stringContains html "data-docs-example-title=\"true\">Primary</h2>" "example toolbar owns its heading"
            Expect.stringContains html "aria-label=\"Copy Primary code\"" "copy action has an example-specific name"
            Expect.stringContains html "data-docs-copy-source=\"true\"" "copy targets the literal source"
            Expect.isLessThan (html.IndexOf(">Preview</button>")) (html.IndexOf(">Code</button>")) "preview is the first tab"
            Expect.equal (System.Text.RegularExpressions.Regex.Matches(html, "role=\"tab\"").Count) 2 "gallery examples expose only preview and complete code"
            Expect.equal (System.Text.RegularExpressions.Regex.Matches(html, "data-docs-copy-source=\"true\"").Count) 1 "the complete source is the only copy target"
        }

        test "Examples provide accessible independent preview and code tabs" {
            let example =
                fveExample { yield _name ("counter-example"); yield _label ("Counter"); yield _variant ExampleVariant.CodeFirst; yield _language ("fsharp"); yield fveExampleCode { yield ("button { \"Increment\" }" : string) }; yield ((button { _type "button"; "Increment" }) : HtmlElement) }
                |> Render.toString

            Expect.stringContains example "data-docs-example=\"true\"" "example marker"
            Expect.stringContains example "role=\"tablist\" aria-label=\"Counter\"" "accessible tab list"
            Expect.stringContains example "role=\"tab\"" "tab semantics"
            Expect.stringContains example "aria-controls=\"counter-example-panel-code\"" "code panel relationship"
            Expect.stringContains example "data-docs-example-preview=\"true\"" "consumer preview"
            Expect.stringContains example "data-docs-example-code=\"true\"" "code panel hook"
            Expect.stringContains example "language-fsharp" "Prism-compatible code"
            Expect.stringContains example "initial-tab=\"code\"" "developer examples show source first"
            Expect.isLessThan
                (example.IndexOf(">Code</button>", System.StringComparison.Ordinal))
                (example.IndexOf(">Preview</button>", System.StringComparison.Ordinal))
                "code appears before preview in the toggle"
            Expect.stringContains example "<fve-code-block " "code panels keep an independent lifecycle owner"
            Expect.stringContains example "aria-label=\"Copy Counter code\"" "every example exposes a direct source copy action"
            Expect.stringContains example "data-docs-copy-source=\"true\"" "every example identifies the exact source to copy"

            let previewFirst =
                fveExample { yield _name ("preview-example"); yield _label ("Preview"); yield _variant ExampleVariant.PreviewFirst; yield _language ("fsharp"); yield fveExampleCode { yield ("div { \"Source\" }" : string) }; yield ((div { "Preview" }) : HtmlElement) }
                |> Render.toString
            Expect.stringContains previewFirst "initial-tab=\"preview\"" "preview-first examples initialize independently"
            Expect.stringContains previewFirst "data-docs-preview-initial=\"true\"" "initial previews opt into lifecycle initialization"

            let codeFirst =
                fveExample { yield _name ("source-example"); yield _label ("Source"); yield _variant ExampleVariant.CodeFirst; yield _language ("fsharp"); yield fveExampleCode { yield ("div { \"Source\" }" : string) }; yield ((div { "Preview" }) : HtmlElement) }
                |> Render.toString
            Expect.stringContains codeFirst "initial-tab=\"code\"" "code-first examples initialize independently"
        }

        test "Version selectors are opt-in navigation" {
            let versions = VersionView.selector "2026.8" [ DocsVersion.create "2026.8" "/"; DocsVersion.create "2026.2" "/v2026.2" ] |> Render.toString

            Expect.stringContains versions "aria-label=\"Documentation version\"" "version selector label"
            Expect.stringContains versions "aria-current=\"page\"" "current version"
        }

        test "Browser frames and tabs are reusable independent components" {
            let framed =
                fveBrowser {
                    yield _address ("https://fve.meiermade.com/components/browser")
                    yield (
                        (div { "Application" })
                    )
                }
                |> Render.toString
            Expect.stringContains framed "data-browser-frame=\"true\"" "browser marker"
            Expect.stringContains framed "data-browser-url=\"https://fve.meiermade.com/components/browser\"" "canonical URL marker"

            let tabs =
                fveTabs {
                    _id "item-states"
                    _ariaLabel "Item states"
                    fveTabsItem { _value "empty"; fveTabsTrigger { "Empty" }; div { "No items" } }
                    fveTabsItem { _value "ready"; fveTabsTrigger { "Ready" }; div { "Items" } }
                } |> Render.toString

            Expect.stringContains tabs "role=\"tablist\"" "tab list semantics"
            Expect.stringContains tabs "aria-controls=\"item-states-panel-" "tab controls panel"
            Expect.stringContains tabs "role=\"tabpanel\"" "tab panel semantics"
        }

        test "Browser and Phone accept host-owned review chrome without owning routes or navigation" {
            let control = a { _href "/components/browser?expanded=1"; "Expand" }
            let browser =
                fveBrowser {
                    _expanded true
                    _address "/components/browser"
                    p { "Product" }
                    fveBrowserWindowControls { control }
                    fveBrowserToolbarActions { span { "Review state" } }
                } |> Render.toString
            Expect.stringContains browser "data-fve-browser-expanded=\"true\"" "viewport presentation is explicit"
            Expect.stringContains browser "data-fve-browser-address=\"true\"" "expansion retains address chrome"
            Expect.stringContains browser "href=\"/components/browser?expanded=1\"" "native host-owned destination survives"
            Expect.stringContains browser "Review state" "trailing controls render"
            Expect.stringContains browser ">Product<" "product remains inside its frame"
            Expect.isFalse (browser.Contains "data-on") "frame presentation installs no competing runtime"
            let phone =
                fvePhone {
                    _expanded true
                    p { "Messages" }
                    fvePhoneToolbar { control }
                } |> Render.toString
            Expect.stringContains phone "data-fve-phone-expanded=\"true\"" "Phone supports viewport presentation"
            Expect.isLessThan (phone.IndexOf "data-fve-phone-toolbar") (phone.IndexOf "data-fve-phone-screen") "review toolbar is outside and above the simulated screen"
            Expect.isFalse (phone.Contains "data-fve-browser-address") "native messages are not given a browser address"
        }

        test "Small display primitives preserve semantic and interaction boundaries" {
            let shortcut = fveKbdShortcut { _ariaLabel "Open search"; for key in [ "Control"; "K" ] do fveKbd { key } } |> Render.toString
            Expect.equal (shortcut.Split("<kbd", System.StringSplitOptions.None).Length - 1) 2 "each shortcut key is semantic kbd markup"
            Expect.stringContains shortcut "aria-label=\"Open search\"" "shortcut group is labelled"
            Expect.isFalse (shortcut.Contains("data-on")) "Kbd registers no behavior"

            let semanticSeparator = fveSeparator { _role "separator" } |> Render.toString
            Expect.stringContains semanticSeparator "role=\"separator\"" "semantic separator has a role"
            Expect.stringContains semanticSeparator "aria-orientation=\"horizontal\"" "semantic orientation is explicit"
            let decorativeSeparator = fveSeparator { _orientation Orientation.Vertical } |> Render.toString
            Expect.stringContains decorativeSeparator "aria-hidden=\"true\"" "decorative separator is ignored"

            let skeletonContent = div { _class "grid gap-2"; fveSkeleton { _class "h-4 w-full" } }
            let skeleton = fveSkeletonRegion { _ariaLabel "Loading records"; skeletonContent } |> Render.toString
            Expect.stringContains skeleton "class=\"grid gap-2\"" "consumer-authored placeholder layout is preserved"
            Expect.throws (fun () -> fveSkeletonRegion { _ariaLabel " "; skeletonContent } |> ignore) "loading region requires a meaningful label"
            Expect.stringContains skeleton "role=\"status\"" "one region owns status"
            Expect.stringContains skeleton "aria-busy=\"true\"" "region is truthfully busy"
            Expect.equal (skeleton.Split("aria-hidden=\"true\"", System.StringSplitOptions.None).Length - 1) 1 "placeholder is silent"
            Expect.stringContains skeleton "motion-reduce:animate-none" "placeholder honors reduced motion"

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
