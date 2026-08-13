namespace FSharp.ViewEngine.Docs

open FSharp.ViewEngine
open System

/// Rich inline documentation content rendered with safe text and typed links.
type DocsInline =
    | InlineText of string
    | InlineCode of string
    | InlineLink of label:string * href:string
    | InlineStrong of DocsInline list
    | InlineEmphasis of DocsInline list

/// A typed documentation content block.
[<NoEquality; NoComparison>]
type DocsBlock =
    | Paragraph of string
    | RichParagraph of DocsInline list
    | Bullets of string list
    | RichBullets of ordered:bool * items:DocsInline list list
    | Table of headers:string list * rows:string list list
    | RichTable of headers:DocsInline list list * rows:DocsInline list list list
    | Code of language:string * source:string
    | Diagram of string
    | C4Diagram of string
    | Sequence of SequenceDiagram.Diagram
    | Callout of label:string * text:string
    | RichCallout of label:DocsInline list * content:DocsInline list
    | Custom of HtmlElement

/// A documentation section with a stable fragment ID and semantic heading level.
[<NoEquality; NoComparison>]
type DocsSection =
    { id:string
      title:string
      level:int
      blocks:DocsBlock list }

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
                @ (section.blocks
                   |> List.collect (function
                       | Table(headers, rows) when rows |> List.exists (fun row -> row.Length <> headers.Length) ->
                           [ issue "page.table-invalid-width" $"A table in section '{section.id}' has a row with the wrong number of cells." ]
                       | RichTable(headers, rows) when rows |> List.exists (fun row -> row.Length <> headers.Length) ->
                           [ issue "page.table-invalid-width" $"A rich table in section '{section.id}' has a row with the wrong number of cells." ]
                       | _ -> [])))

        let duplicateSectionIssues =
            page.sections
            |> List.countBy _.id
            |> List.choose (fun (id, count) ->
                if count > 1 then Some(issue "page.duplicate-section-id" $"Duplicate section ID: {id}.") else None)

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
        @ pagerIssues
