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
    { href:string
      page:DocumentationPageConfig
      keywords:string list
      group:string }

/// <category>navigation</category>
[<RequireQualifiedAccess>]
module DocsSearchEntry =
    let create href page keywords : DocsSearchEntry =
        { href = href; page = page; keywords = keywords; group = "Search results" }
    /// Groups the page result. Its section deep links remain under Search results.
    let withGroup group entry = { entry with group = group }

/// <summary>A normalized local search result for a page or section.</summary>
/// <category>navigation</category>
type DocsSearchResult =
    { title:string
      description:string
      href:string
      keywords:string list
      group:string }

/// <category>navigation</category>
module DocsSearch =
    /// Expands page entries into grouped page results and focused section deep links.
    let index (entries:DocsSearchEntry list) =
        entries
        |> List.collect (fun entry ->
            { title = entry.page.title
              description = entry.page.description
              href = entry.href
              keywords = entry.keywords
              group = entry.group }
            :: (entry.page.sections
                |> List.map (fun section ->
                    { title = $"{entry.page.title} · {section.title}"
                      description = ""
                      href = $"{entry.href}#{section.id}"
                      keywords = [ section.title ]
                      group = "Search results" })))

/// <category>navigation</category>
module SearchView =
    let render (results:DocsSearchResult list) =
        let groups =
            results
            |> List.groupBy _.group
            |> List.sortBy (fun (group, _) -> if group = "Components" then 0 else 1)
            |> List.map (fun (group, entries) ->
                CommandGroup.create group (entries |> List.map (fun result ->
                    CommandItem.link result.href result.title
                    |> CommandItem.withKeywords (result.description :: result.keywords))))
        let command =
            Command.create "docs-search" "Search documentation" groups
            |> Command.withPlaceholder "Search documentation…"
        div {
            _id "docs-search-shell"
            _class "relative"
            _dataOn ("keydown__window", "if ((evt.metaKey || evt.ctrlKey) && evt.key.toLowerCase() === 'k') { evt.preventDefault(); const dialog = document.getElementById('docs-search-dialog'); if (!dialog.open) dialog.showModal(); document.getElementById('docs-search-input').focus() }")
            button {
                for attribute in Command.triggerAttributes command do attribute
                _ariaLabel "Search documentation"
                _type "button"
                _class "flex h-8 cursor-pointer items-center gap-2 rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-[var(--fve-surface)] px-2.5 text-sm text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] max-sm:w-8 max-sm:justify-center max-sm:p-0"
                svg {
                    _viewBox "0 0 24 24"
                    _fill "none"
                    _ariaHidden true
                    _class "size-4 sm:hidden"
                    _attr ("stroke", "currentColor")
                    _attr ("stroke-width", "1.5")
                    path { _attr ("stroke-linecap", "round"); _attr ("stroke-linejoin", "round"); _d "m21 21-4.34-4.34M11 19a8 8 0 1 1 0-16 8 8 0 0 1 0 16Z" }
                }
                span { _class "hidden sm:inline"; "Search" }
                kbd {
                    _class "hidden rounded border border-[var(--fve-border)] px-1 py-px font-mono text-xs sm:inline"
                    _dataText "navigator.userAgent.includes('Mac') ? '⌘K' : 'Ctrl+K'"
                    "Ctrl+K"
                }
            }
            Command.renderDialog id command
        }
