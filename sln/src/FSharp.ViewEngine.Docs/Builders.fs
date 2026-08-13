namespace FSharp.ViewEngine.Docs

open FSharp.ViewEngine

[<AutoOpen>]
module Builders =
    let docsRegisteredPage path aliases page : DocsRegisteredPage =
        { path = path; aliases = aliases; page = page }

    let docsSearchEntry href page keywords : DocsSearchEntry =
        { href = href; page = page; keywords = keywords }

    let docsSearchDialog index = SearchView.render index

    let docsStory id title language source preview : DocsStory =
        { id = id; title = title; language = language; source = source; preview = preview; viewports = []; themes = []; states = [] }
    let docsStoryWithViewports viewports story = { story with viewports = viewports }
    let docsStoryWithThemes themes story = { story with themes = themes }
    let docsStoryWithStates states story = { story with states = states }
    let docsStoryCatalog stories = StoryView.catalog stories
    let docsVersion label href : DocsVersion = { label = label; href = href }
    let docsVersionSelector current versions = StoryView.versionSelector current versions

    let docsNavPage id label href destination = Nav.page id label href destination
    let docsNavGroup id label defaultOpen children = Nav.group id label defaultOpen children
    let docsNavGroupWithBreadcrumb id label breadcrumbHref defaultOpen children =
        Nav.groupWithBreadcrumb id label breadcrumbHref defaultOpen children

    let docsSection id title blocks : DocsSection =
        { id = id
          title = title
          level = 2
          blocks = blocks }

    let docsSubsection id title blocks : DocsSection =
        { id = id
          title = title
          level = 3
          blocks = blocks }

    let docsText text = InlineText text
    let docsInlineCode source = InlineCode source
    let docsLink label href = InlineLink(label, href)
    let docsStrong content = InlineStrong content
    let docsEmphasis content = InlineEmphasis content

    let docsParagraph text = Paragraph text
    let docsRichParagraph content = RichParagraph content
    let docsBullets items = Bullets items
    let docsRichBullets items = RichBullets(false, items)
    let docsOrderedItems items = RichBullets(true, items)
    let docsTable headers rows = Table(headers, rows)
    let docsRichTable headers rows = RichTable(headers, rows)
    let docsCode language source = Code(language, source)
    let docsDiagram source = Diagram source
    let docsC4Diagram source = C4Diagram source
    let docsSequence diagram = Sequence diagram
    let docsCallout label text = Callout(label, text)
    let docsRichCallout label content = RichCallout(label, content)
    let docsCustom element = Custom element

    let docsRail content = CustomRail content

    let docsPageLink label href : DocsPageLink =
        { label = label; href = href }

    let docsPager previousPage nextPage : DocsPager =
        { previousPage = previousPage; nextPage = nextPage }

    let docsWithPager pager page =
        { page with pager = Some pager }

    let docsWithHeadingAdornment adornment page =
        { page with headingAdornment = Some adornment }

    /// Applies browser, canonical, social, version, and maintenance metadata to a page.
    let docsWithMetadata metadata page =
        { page with metadata = metadata }

    let docsArticle activeId title description sections =
        DocsPage.create activeId title description Visible Article TableOfContents sections

    let docsReference activeId title description sections rightRail =
        DocsPage.create activeId title description Visible Reference rightRail sections

    let docsCanvas activeId title description sections =
        DocsPage.create activeId title description Visible Canvas NoRail sections

    let docsCanvasWithHiddenHeading activeId title description sections =
        DocsPage.create activeId title description VisuallyHidden Canvas NoRail sections

    let docsDocument site page = DocsView.document site page
    let docsPage site page = DocsView.page site page
    let docsPageContent site page = DocsView.pageContent site page
    let docsSideNav site page = DocsView.sideNav site page
    let docsContent page = DocsView.content page

    let docsBrowserFrame canonicalUrl content = Wireframe.browserFrame canonicalUrl content
    let docsStateTabs id label states = Wireframe.stateTabs id label states
    let docsExample id label language source preview = Example.codeFirst id label language source preview
    let docsExampleCodeFirst id label language source preview = Example.codeFirst id label language source preview

    let docsApiEndpoint method path description = ApiReference.endpoint method path description
    let docsParameter name typeName required description = ApiReference.parameter name typeName required description
    let docsParameters parameters = ApiReference.parameters parameters
    let docsApiParameter name typeName location required defaultValue enumValues example description : DocsApiParameter =
        { name = name; typeName = typeName; location = location; required = required; defaultValue = defaultValue; enumValues = enumValues; example = example; description = description }
    let docsApiResponse status description language example : DocsApiResponse =
        { status = status; description = description; language = language; example = example }
    let docsApiError code description : DocsApiError = { code = code; description = description }
    let docsApiOperation method path description authentication parameters responses errors idempotency apiVersion deprecated =
        ApiReference.operation method path description authentication parameters responses errors idempotency apiVersion deprecated
    let docsCodeExample title language source = ApiReference.codeExample title language source
    let docsResponseExample status language source = ApiReference.responseExample status language source
