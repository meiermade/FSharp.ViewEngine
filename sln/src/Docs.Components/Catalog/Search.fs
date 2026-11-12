namespace FSharp.ViewEngine.Components.Templates

open FSharp.ViewEngine.Components

open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Html
open type Svg
open type Datastar

/// <summary>Consumer-provided search metadata for a documentation page.</summary>
/// <category>navigation</category>
type DocsSearchEntry =
    { href: string
      page: DocumentationPageConfig
      keywords: string list
      group: string
      newTab: bool }

/// <category>navigation</category>
[<RequireQualifiedAccess>]
module DocsSearchEntry =
    let create href page keywords : DocsSearchEntry =
        { href = href
          page = page
          keywords = keywords
          group = "Pages"
          newTab = false }

    /// Groups page results; contextual section deep links remain secondary under Sections.
    let withGroup group entry = { entry with group = group }
    let inNewTab entry = { entry with newTab = true }

/// <summary>A normalized local search result for a page or section.</summary>
/// <category>navigation</category>
type DocsSearchResult =
    { title: string
      description: string
      href: string
      keywords: string list
      group: string
      sectionTitle: string option
      newTab: bool }

/// <category>navigation</category>
module DocsSearch =
    /// Expands page entries into grouped page results and focused section deep links.
    let index (entries: DocsSearchEntry list) =
        entries
        |> List.collect (fun entry ->
            { title = entry.page.title
              description = entry.page.description
              href = entry.href
              keywords = entry.keywords
              group = entry.group
              sectionTitle = None
              newTab = entry.newTab }
            :: (entry.page.sections
                |> List.map (fun section ->
                    { title = $"{entry.page.title} · {section.title}"
                      description = ""
                      href = $"{entry.href}#{section.id}"
                      keywords = [ section.title ]
                      group = "Sections"
                      sectionTitle = Some section.title
                      newTab = entry.newTab })))

/// <category>navigation</category>
module SearchView =
    let render (results: DocsSearchResult list) =
        let groups =
            results
            |> List.groupBy _.group
            |> List.sortBy (fun (group, _) ->
                match group with "Pages" -> 0 | "Components" -> 1 | "Guides" -> 2 | "Examples" -> 3 | "Sections" -> 4 | _ -> 5)
            |> List.map (fun (group, entries) ->
                fveCommandGroup {
                    _label group

                    yield!
                        (entries
                         |> List.map (fun result ->
                             let icon =
                                 match result.sectionTitle, group with
                                 | Some _, _ -> IconName.Hashtag
                                 | None, "Components" -> IconName.Squares2x2
                                 | None, "Guides" -> IconName.BookOpen
                                 | None, "Examples" -> IconName.ComputerDesktop
                                 | _ -> IconName.DocumentText
                             fveCommandItem {
                                 _href result.href
                                 _label result.title
                                 _keywords (result.description :: result.keywords)
                                 match result.sectionTitle with Some title -> _matchAnchor title | None -> ()
                                 if result.newTab then
                                     _target "_blank"
                                     _rel "noopener"
                                     _ariaLabel (result.title + " (opens in a new tab)")
                                 fveCommandItemLeading { fveIcon { _name icon; _variant IconVariant.Mini } }
                                 span {
                                     _class "flex min-w-0 items-center gap-2"
                                     span { _class "min-w-0 flex-1 [overflow-wrap:anywhere]"; result.title }
                                     if result.newTab then
                                         fveIcon { _name IconName.ArrowTopRightOnSquare; _variant IconVariant.Mini; _size Size.ExtraSmall; _class "shrink-0" }
                                 }
                             }))
                })

        let command =
            fveCommandDialog {
                _id "docs-search"
                _ariaLabel "Search documentation"
                yield! groups
                _placeholder "Search documentation…"
            }

        div {
            _id "docs-search-shell"
            _class "relative whitespace-normal"

            _dataOn (
                "keydown__window",
                "if ((evt.metaKey || evt.ctrlKey) && evt.key.toLowerCase() === 'k') { evt.preventDefault(); const dialog = document.getElementById('docs-search-dialog'); const input = document.getElementById('docs-search-input'); input.value = ''; input.dispatchEvent(new Event('input', { bubbles: true })); if (!dialog.open) document.getElementById('docs-search-dialog-trigger').click(); input.focus() }"
            )

            button {
                for attribute in Command.triggerAttributes "docs-search" do
                    attribute

                _ariaLabel "Search documentation"
                _type "button"

                _class
                    "flex h-8 cursor-pointer items-center gap-2 rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-[var(--fve-surface)] px-2.5 text-sm text-[var(--fve-muted-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] max-sm:w-8 max-sm:justify-center max-sm:p-0"

                fveIcon { _name IconName.MagnifyingGlass; _size Size.Small }

                kbd {
                    _class "hidden rounded border border-[var(--fve-border)] px-1 py-px font-mono text-xs sm:inline"
                    _dataText "navigator.userAgent.includes('Mac') ? '⌘K' : 'Ctrl+K'"
                    "Ctrl+K"
                }
            }

            command
        }
