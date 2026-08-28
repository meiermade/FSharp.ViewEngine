module DocsTests

open Expecto
open FSharp.ViewEngine
open FSharp.ViewEngine.Docs
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
    [ docsNavPage "home" "Overview" "/" Home
      docsNavGroupWithBreadcrumb "guides" "Guides" "/guides" true [
          docsNavPage "guide" "Overview" "/guides" Guide
          docsNavPage "detail" "Detail" "/guides/detail" Detail ]
      docsNavPage "reference" "API reference" "/reference" Reference ]

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
            let guide = docsArticle "guide" "Guide" "Guide description" [] |> docsWithPager (docsPager None (Some(docsPageLink "Missing" "/missing")))
            let orphan = docsArticle "orphan" "Orphan" "" []
            let registry = [ docsRegisteredPage "/guide" [ "/start" ] guide; docsRegisteredPage "/orphan" [ "/guide" ] orphan ]
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
                [ docsNavPage "home" "Start" "/" Home
                  docsNavGroup "commands" "Commands" false [
                      docsNavPage "run" "Run" "/commands/run" Guide ] ]

            Expect.isEmpty (Navigation.validate customNavigation) "groups do not require overview pages or breadcrumb links"

            let invalid =
                [ docsNavPage "home" "Home" "/" Home
                  docsNavPage "home" "Duplicate" "/" Guide
                  docsNavGroup "empty" "Empty" false [] ]

            let codes = Navigation.validate invalid |> issueCodes
            Expect.isTrue (Set.contains "navigation.duplicate-id" codes) "duplicate IDs"
            Expect.isTrue (Set.contains "navigation.duplicate-href" codes) "duplicate hrefs"
            Expect.isTrue (Set.contains "navigation.empty-group" codes) "empty groups"
        }

        test "Rich inline content renders in paragraphs, lists, tables, and callouts" {
            let richText = [ docsText "Read "; docsStrong [ docsText "carefully" ]; docsText " and "; docsLink "continue" "/next"; docsText " with "; docsInlineCode "docsArticle" ]
            let page =
                docsArticle "guide" "Guide" "Description" [
                    docsSection "content" "Content" [
                        docsRichParagraph richText
                        docsOrderedItems [ richText ]
                        docsRichBullets [ richText ]
                        docsRichTable [ [ docsText "Builder" ]; [ docsText "Purpose" ] ] [ [ [ docsInlineCode "docsArticle" ]; [ docsText "Guides" ] ] ]
                        docsRichCallout [ docsText "Note" ] richText ] ]

            let rendered = docsContent page |> Render.toString
            Expect.stringContains rendered "<strong>carefully</strong>" "strong inline content"
            Expect.stringContains rendered "href=\"/next\"" "inline link"
            Expect.stringContains rendered "<code>docsArticle</code>" "inline code"
            Expect.stringContains rendered "<ol class=\"spec-bullets\"" "ordered list"
            Expect.stringContains rendered "<ul class=\"spec-bullets\"" "unordered list"
            Expect.stringContains rendered "<th>Builder</th>" "rich table header"
            Expect.stringContains rendered "class=\"spec-callout-label\">Note" "rich callout label"
        }

        test "Heading adornments preserve the semantic page heading" {
            let page =
                docsArticle "home" "Home" "Description" []
                |> docsWithHeadingAdornment (img { _src "/logo.svg"; _alt "" })
            let rendered = docsContent page |> Render.toString

            Expect.stringContains rendered "src=\"/logo.svg\"" "adornment is rendered before the title"
            Expect.equal (rendered.Split("<h1").Length - 1) 1 "page retains one semantic h1"
        }

        test "Article, reference, and canvas builders select composable layouts" {
            let sections = [ docsSection "usage" "Usage" [ docsCode "shell" "example run"; docsParagraph "Any page shape is allowed." ] ]
            let article = docsArticle "guide" "CLI guide" "Run the CLI." sections
            let reference = docsReference "reference" "Create item" "Creates an item." sections (docsRail (div { "Request examples" }))
            let canvas = docsCanvas "detail" "Architecture" "Explore the system." sections
            let hiddenCanvas = docsCanvasWithHiddenHeading "detail" "Web workflow" "Use the web application." sections

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
                docsPager
                    (Some(docsPageLink "Introduction" "/"))
                    (Some(docsPageLink "Usage" "/usage"))

            let page =
                docsArticle "guide" "Guide" "Description" []
                |> docsWithPager pager

            let rendered = docsDocument site page |> Render.toString
            Expect.stringContains rendered "aria-label=\"Page navigation\"" "pager landmark"
            Expect.stringContains rendered "rel=\"prev\" href=\"/\"" "previous destination"
            Expect.stringContains rendered "rel=\"next\" href=\"/usage\"" "next destination"
            Expect.stringContains rendered "Previous" "previous direction"
            Expect.stringContains rendered ">Introduction</span>" "previous page title"
            Expect.stringContains rendered "Next" "next direction"
            Expect.stringContains rendered ">Usage</span>" "next page title"
            Expect.stringContains rendered "window.fsharpDocsNavigation.begin()" "pager uses Docs navigation lifecycle"
        }

        test "Search indexes include page titles, headings, descriptions, and consumer keywords" {
            let page = docsArticle "guide" "Build a view" "Compose typed HTML." [ docsSection "render" "Render output" [] ]
            let entry = docsSearchEntry "/guide" page [ "serialization" ]
            let index = DocsSearch.index [ entry ]
            let rendered = docsSearchDialog index |> Render.toString

            Expect.sequenceEqual index[0].keywords [ "serialization" ] "consumer keywords"
            Expect.stringContains rendered "aria-label=\"Search documentation\"" "search dialog label"
            Expect.stringContains rendered "data-docs-search-entry" "search result metadata"
            Expect.stringContains rendered "href=\"/guide#render\"" "heading deep link"
            Expect.stringContains rendered "Ctrl+K" "keyboard shortcut hint"
        }

        test "Page metadata controls browser, search, and social metadata" {
            let page =
                docsArticle "guide" "Guide" "A focused guide description." []
                |> docsWithMetadata {
                    DocsPageMetadata.defaults with
                        browserTitle = Some "Guide · Example"
                        canonicalUrl = Some "https://docs.example.com/canonical-guide"
                        noIndex = true
                        socialImage = Some "https://docs.example.com/guide.png"
                        version = Some "2026.8"
                        deprecated = true
                        lastUpdated = Some "2026-08-12"
                        editUrl = Some "https://github.com/example/docs/edit/main/guide.fs" }

            let rendered = docsDocument site page |> Render.toString
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
                docsCanvasWithHiddenHeading "detail" "Detail" "A customizable page." [
                    docsSection "diagram" "Diagram" [ docsSequence (sequenceDiagram ()) ]
                    docsSection "custom" "Custom" [ docsCustom (div { _data("example", "true"); "Product content" }) ] ]

            let rendered = docsDocument site page |> Render.toString
            Expect.stringContains rendered "<style>" "default component styles are self-contained"
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
            Expect.stringContains rendered "id=\"spec-color-mode-button\"" "built-in color mode selector"
            Expect.stringContains rendered "role=\"menuitemradio\"" "color mode options use menu semantics"
            Expect.stringContains rendered "window.fsharpDocsColorMode" "color mode is applied before paint and persisted"
            Expect.stringContains rendered "--docs-code-bg:#f6f8fa" "light mode uses a light code surface"
            Expect.stringContains rendered "--docs-code-bg:#0d1117" "dark mode uses a dark code surface"
            Expect.stringContains rendered ".spec-document .token.atrule" "embedded Prism tokens follow the active color mode"
            Expect.stringContains rendered ".spec-document pre.spec-code code{background:transparent" "package code selector overrides host styles"
            Expect.stringContains rendered "font-size:.8125rem;line-height:1.55;text-shadow:none" "code uses compact typography"
            Expect.stringContains rendered "rel=\"canonical\" href=\"https://docs.example.com/guides/detail\"" "canonical page URL"
            Expect.stringContains rendered "name=\"description\" content=\"A customizable page.\"" "page-specific description"
        }

        test "Repository links and default color modes remain consumer configurable" {
            let configuredSite =
                { site with
                    repository = Some(DocsRepository.link "Source repository" "https://code.example.com/project")
                    defaultColorMode = DocsColorMode.Dark }

            let rendered = docsDocument configuredSite (docsArticle "guide" "Guide" "Description" []) |> Render.toString
            Expect.stringContains rendered "href=\"https://code.example.com/project\"" "custom repository URL"
            Expect.stringContains rendered ">Source repository</a>" "custom repository label"
            Expect.stringContains rendered "defaultMode: \"dark\"" "consumer default is serialized before paint"
            Expect.stringContains rendered "role=\"menuitemradio\" tabindex=\"-1\" aria-checked=\"true\"" "default option is checked in server HTML"
        }

        test "Rich API operations cover authentication, located parameters, responses, errors, and policy metadata" {
            let operation =
                docsApiOperation
                    POST
                    "/v1/items/{id}"
                    "Update an item"
                    (Some "Bearer token")
                    [ docsApiParameter "id" "string" Path true None None (Some "item_123") "Item identifier."
                      docsApiParameter "mode" "string" Query false (Some "safe") (Some [ "safe"; "force" ]) None "Update mode." ]
                    [ docsApiResponse "200" "Updated" (Some "json") (Some "{ \"id\": \"item_123\" }")
                      docsApiResponse "404" "Not found" None None ]
                    [ docsApiError "item_not_found" "The item does not exist." ]
                    (Some "Requests are idempotent for 24 hours.")
                    (Some "2026-08-01")
                    true
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
            let endpoint = docsApiEndpoint POST "/v1/items" "Creates an item." |> Render.toString
            let parameters =
                docsParameters [
                    docsParameter "name" "string" true "The display name."
                    docsParameter "metadata" "object" false "Additional values." ]
                |> Render.toString
            let request = docsCodeExample "Create item" "curl" "curl --request POST https://api.example.com/v1/items" |> Render.toString
            let response = docsResponseExample "201" "json" "{ \"id\": \"item_123\" }" |> Render.toString

            Expect.stringContains endpoint "data-http-method=\"POST\"" "method metadata"
            Expect.stringContains endpoint "/v1/items" "endpoint path"
            Expect.stringContains parameters "name" "parameter name"
            Expect.stringContains parameters "Required" "required marker"
            Expect.stringContains request "Create item" "request example title"
            Expect.stringContains response "201" "response status"
        }

        test "Pages declare optional runtime asset requirements" {
            let plain = docsArticle "guide" "Guide" "Description" []
            let diagramSource = "flowchart LR\nA[\"<script>alert('diagram')</script>\"] --> B[Ready]"
            let diagram = docsArticle "guide" "Guide" "Description" [ docsSection "flow" "Flow" [ docsDiagram diagramSource ] ]
            let highlighted = docsArticle "guide" "Guide" "Description" [ docsSection "code" "Code" [ docsCode "fsharp" "let x = 1" ] ]

            let plainHtml = docsDocument site plain |> Render.toString
            let diagramHtml = docsDocument site diagram |> Render.toString
            let highlightedHtml = docsDocument site highlighted |> Render.toString
            Expect.stringContains plainHtml "window.fsharpDocsMermaid" "plain pages configure lazy Mermaid for later navigation"
            Expect.stringContains plainHtml "/scripts/mermaid.11.16.0.min.js" "plain pages retain the configured Mermaid source"
            Expect.isFalse (plainHtml.Contains("src=\"/scripts/mermaid.11.16.0.min.js\"")) "plain pages do not eagerly load Mermaid"
            Expect.isFalse (plainHtml.Contains("prism-tomorrow.1.29.0.min.css")) "default Prism colors are embedded rather than loaded from a dark-only stylesheet"
            Expect.stringContains plainHtml "--docs-code-green:#116329" "light Prism palette is available before highlighting"
            Expect.stringContains plainHtml "--docs-code-green:#7ee787" "dark Prism palette is available before highlighting"
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

        test "CSP nonces apply to package-owned inline scripts and styles" {
            let configuredSite = { site with assets = { DocsAssets.defaults with nonce = Some "request-nonce" } }
            let rendered = docsDocument configuredSite (docsArticle "guide" "Guide" "Description" []) |> Render.toString

            Expect.stringContains rendered "<style nonce=\"request-nonce\">" "default style nonce"
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
            let page = docsArticle "guide" "Guide" "Description" [ docsSection "flow" "Flow" [ docsDiagram "flowchart LR" ] ]
            let rendered = docsDocument configuredSite page |> Render.toString

            Expect.stringContains rendered "href=\"/css/product.css\"" "product stylesheet"
            Expect.stringContains rendered "href=\"/css/custom-prism.css\"" "consumer Prism stylesheet overrides the embedded palette"
            Expect.stringContains rendered "securityLevel: \"strict\"" "Mermaid security setting is safely serialized"
            Expect.stringContains rendered "typeof window.mermaid?.initialize === 'function'" "Mermaid API detection cannot be clobbered by a consumer element ID"
            Expect.stringContains rendered "suppressErrorRendering: true" "Mermaid error SVGs are suppressed in favor of package-owned failure content"
            Expect.stringContains rendered "window.mermaid.render(id, node.dataset.mermaidSource" "Mermaid renders from encoded source without restoring visible raw text"
            Expect.stringContains rendered "mermaidRenderQueue" "Mermaid renders are serialized"
            Expect.stringContains rendered "setMermaidFailed" "asset and render failures use the shared deterministic failure state"
            Expect.stringContains rendered "name=\"robots\"" "additional head content"
        }

        test "Code blocks and API examples provide accessible copy controls" {
            let page = docsArticle "guide" "Guide" "Description" [ docsSection "code" "Code" [ docsCode "fsharp" "let value = 42" ] ]
            let document = docsDocument site page |> Render.toString
            let api = docsCodeExample "Request" "curl" "curl https://example.com" |> Render.toString

            Expect.stringContains document "aria-label=\"Copy code\"" "standard code copy button"
            Expect.stringContains document "data-docs-copy-source" "standard source relationship"
            Expect.stringContains api "aria-label=\"Copy Request\"" "API code copy button"
            Expect.stringContains document "window.fsharpDocsCopy" "document copy lifecycle"
        }

        test "Examples provide accessible independent preview and code tabs" {
            let example =
                docsExample "counter-example" "Counter" "fsharp" "button { \"Increment\" }" (button { _type "button"; "Increment" })
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

            let codeFirst =
                docsExampleCodeFirst "source-example" "Source" "fsharp" "div { \"Source\" }" (div { "Preview" })
                |> Render.toString
            Expect.stringContains codeFirst "source_exampleExample: &#39;code&#39;" "code-first examples initialize independently"
        }

        test "Story catalogs and version selectors render opt-in metadata" {
            let story =
                docsStory "notice" "Notice" "fsharp" "div { \"Saved\" }" (div { "Saved" })
                |> docsStoryWithViewports [ DocsViewport.Mobile; DocsViewport.Desktop ]
                |> docsStoryWithThemes [ DocsColorMode.Light; DocsColorMode.Dark ]
                |> docsStoryWithStates [ "ready"; "error" ]
            let catalog = docsStoryCatalog [ story ] |> Render.toString
            let versions = docsVersionSelector "2026.8" [ docsVersion "2026.8" "/"; docsVersion "2026.2" "/v2026.2" ] |> Render.toString

            Expect.stringContains catalog "data-docs-story=\"notice\"" "story identity"
            Expect.stringContains catalog "data-docs-viewports=\"mobile desktop\"" "viewport metadata"
            Expect.stringContains catalog "data-docs-themes=\"light dark\"" "theme metadata"
            Expect.stringContains catalog "data-docs-states=\"ready error\"" "state metadata"
            Expect.stringContains versions "aria-label=\"Documentation version\"" "version selector label"
            Expect.stringContains versions "aria-current=\"page\"" "current version"
        }

        test "Browser frames and state tabs are reusable independent components" {
            let framed = docsBrowserFrame "https://example.com/items" (div { "Application" }) |> Render.toString
            Expect.stringContains framed "data-browser-frame=\"true\"" "browser marker"
            Expect.stringContains framed "data-browser-url=\"https://example.com/items\"" "canonical URL marker"

            let tabs =
                docsStateTabs "item-states" "Item states" [
                    { id = "empty"; label = "Empty"; content = div { "No items" } }
                    { id = "ready"; label = "Ready"; content = div { "Items" } } ]
                |> Render.toString

            Expect.stringContains tabs "role=\"tablist\"" "tab list semantics"
            Expect.stringContains tabs "aria-controls=\"item-states-panel-empty\"" "tab controls panel"
            Expect.stringContains tabs "role=\"tabpanel\"" "tab panel semantics"
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
