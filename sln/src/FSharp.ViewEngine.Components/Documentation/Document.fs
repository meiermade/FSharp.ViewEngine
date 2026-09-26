namespace FSharp.ViewEngine.Components.Documentation

open FSharp.ViewEngine
open System

/// A documentation section with a stable fragment ID, semantic heading level, and typed HTML content.
[<NoEquality; NoComparison>]
type DocsSection =
    { id:string
      title:string
      level:int
      content:HtmlElement list }

/// Immutable builders for a documentation section.
[<RequireQualifiedAccess>]
module DocumentationSection =
    let create id title content : DocsSection =
        { id = id
          title = title
          level = 2
          content = content }

    let withLevel level (section:DocsSection) = { section with level = level }

type Heading =
    | Visible
    | VisuallyHidden

/// The persisted light, dark, or operating-system color-mode preference.
type DocsColorMode =
    | System
    | Light
    | Dark

module DocsColorMode =
    let value = function
        | System -> "system"
        | Light -> "light"
        | Dark -> "dark"

type DocsLayout =
    | Article
    | Reference
    | Canvas
    /// Full-width examples whose own headers provide section headings.
    | Gallery

[<NoEquality; NoComparison>]
type DocsRightRail =
    | TableOfContents
    | NoRail
    | CustomRail of HtmlElement

type DocsPageLink =
    { label:string
      href:string }

type DocsPager =
    { previousPage:DocsPageLink option
      nextPage:DocsPageLink option }

[<RequireQualifiedAccess>]
module DocsPageLink =
    let create label href : DocsPageLink = { label = label; href = href }

[<RequireQualifiedAccess>]
module DocsPager =
    let create previousPage nextPage : DocsPager =
        { previousPage = previousPage
          nextPage = nextPage }

/// Optional metadata used by document heads, search indexes, and visible maintenance details.
type DocsPageMetadata =
    { browserTitle:string option
      canonicalUrl:string option
      noIndex:bool
      socialImage:string option
      version:string option
      deprecated:bool
      lastUpdated:string option
      editUrl:string option }

module DocsPageMetadata =
    /// Metadata defaults for pages that need only a title and description.
    let defaults =
        { browserTitle = None
          canonicalUrl = None
          noIndex = false
          socialImage = None
          version = None
          deprecated = false
          lastUpdated = None
          editUrl = None }

/// A composable article, reference, or canvas page rendered by the documentation shell.
[<NoEquality; NoComparison>]
type DocsPage =
    { activeId:string
      title:string
      description:string
      heading:Heading
      layout:DocsLayout
      rightRail:DocsRightRail
      sections:DocsSection list
      headingAdornment:HtmlElement option
      pager:DocsPager option
      fixtures:FixtureConfig list
      metadata:DocsPageMetadata }

module DocsPage =
    let create activeId title description heading layout rightRail sections =
        { activeId = activeId
          title = title
          description = description
          heading = heading
          layout = layout
          rightRail = rightRail
          sections = sections
          headingAdornment = None
          pager = None
          fixtures = []
          metadata = DocsPageMetadata.defaults }

    let validate page =
        let required value code message =
            if String.IsNullOrWhiteSpace value then [ issue code message ] else []

        let sectionIssues =
            page.sections
            |> List.collect (fun section ->
                required section.id "page.section-missing-id" "Section IDs cannot be empty."
                @ required section.title "page.section-missing-title" $"Section '{section.id}' must have a title."
                @ [ if section.level < 2 || section.level > 4 then
                        issue "page.section-invalid-level" $"Section '{section.id}' must use heading level 2, 3, or 4." ]
                )

        let duplicateSectionIssues =
            page.sections
            |> List.countBy _.id
            |> List.choose (fun (id, count) ->
                if count > 1 then Some(issue "page.duplicate-section-id" $"Duplicate section ID: {id}.") else None)

        let fixtureIssues =
            page.fixtures
            |> List.countBy Fixture.id
            |> List.choose (fun (id, count) ->
                if count > 1 then Some(issue "page.duplicate-fixture-id" $"Duplicate Fixture ID: {id}.") else None)

        let pagerIssues =
            match page.pager with
            | None -> []
            | Some pager ->
                [ pager.previousPage; pager.nextPage ]
                |> List.choose id
                |> List.collect (fun link ->
                    required link.label "page.pager-missing-label" "Page navigation labels cannot be empty."
                    @ required link.href "page.pager-missing-href" $"Page navigation link '{link.label}' must have a destination.")

        required page.activeId "page.missing-active-id" "Page active IDs cannot be empty."
        @ required page.title "page.missing-title" "Pages must have a title."
        @ required page.description "page.missing-description" $"Page '{page.activeId}' must have a description."
        @ sectionIssues
        @ duplicateSectionIssues
        @ fixtureIssues
        @ pagerIssues
