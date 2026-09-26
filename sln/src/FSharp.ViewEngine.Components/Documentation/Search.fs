namespace FSharp.ViewEngine.Components.Documentation

open FSharp.ViewEngine
open System
open System.Text.Json
open type Html

/// Consumer-provided search metadata for a documentation page.
type DocsSearchEntry =
    { href:string
      page:DocsPage
      keywords:string list }

[<RequireQualifiedAccess>]
module DocsSearchEntry =
    let create href page keywords : DocsSearchEntry =
        { href = href
          page = page
          keywords = keywords }

/// A normalized local search result for a page or section.
type DocsSearchResult =
    { title:string
      description:string
      href:string
      keywords:string list }

module DocsSearch =
    /// Expands page entries into page and section deep-link results.
    let index (entries:DocsSearchEntry list) =
        entries
        |> List.collect (fun entry ->
            { title = entry.page.title
              description = entry.page.description
              href = entry.href
              keywords = entry.keywords }
            :: (entry.page.sections
                |> List.map (fun section ->
                    { title = $"{entry.page.title} · {section.title}"
                      description = entry.page.description
                      href = $"{entry.href}#{section.id}"
                      keywords = section.title :: entry.keywords })))

module SearchView =
    let render (results:DocsSearchResult list) =
        let searchable (result:DocsSearchResult) =
            String.concat " " (result.title :: result.description :: result.keywords)
            |> fun value -> value.ToLowerInvariant()

        let filterScript =
            "const query = evt.currentTarget.value.trim().toLowerCase(); document.querySelectorAll('[data-docs-search-entry]').forEach(entry => entry.hidden = query !== '' && !entry.dataset.docsSearchText.includes(query)); document.getElementById('docs-search-results').hidden = false"

        div {
            _class "relative"
            button {
                _id "docs-search-button"
                _type "button"
                _ariaLabel "Search documentation"
                _ariaHaspopup "dialog"
                _class "flex h-8 cursor-pointer items-center gap-2 rounded-lg border border-[var(--fve-border)] bg-[var(--fve-surface)] px-2.5 text-sm text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] max-sm:w-8 max-sm:justify-center max-sm:p-0 max-sm:[&>kbd]:hidden max-sm:[&>span]:hidden before:max-sm:content-['⌕']"
                _data("on:click", "document.getElementById('docs-search-dialog').showModal(); queueMicrotask(() => document.getElementById('docs-search-input').focus())")
                span { "Search" }
                kbd { _class "rounded border border-[var(--fve-border)] px-1 py-px font-mono text-xs"; "Ctrl+K" }
            }
            dialog {
                _id "docs-search-dialog"
                _ariaLabel "Search documentation"
                _class "max-h-[min(42rem,calc(100vh-4rem))] w-[min(40rem,calc(100vw-2rem))] rounded-[0.875rem] border-0 bg-transparent p-0 text-[var(--fve-text)] shadow-[0_24px_70px_rgb(0_0_0/35%)] backdrop:bg-black/55 backdrop:backdrop-blur-[2px]"
                _data("on:click", "evt.target === evt.currentTarget && evt.currentTarget.close()")
                div {
                    _class "overflow-hidden rounded-[0.875rem] border border-[var(--fve-border)] bg-[var(--fve-surface)]"
                    div {
                        _class "flex gap-2 border-b border-[var(--fve-border)] p-3 [&>button]:cursor-pointer [&>button]:border-0 [&>button]:bg-transparent [&>button]:text-sm [&>button]:text-[var(--fve-muted-text)] [&>input]:min-w-0 [&>input]:flex-1 [&>input]:rounded-lg [&>input]:border [&>input]:border-[var(--fve-border)] [&>input]:bg-[var(--fve-surface)] [&>input]:px-3 [&>input]:py-2.5 [&>input]:text-[var(--fve-text)]"
                        input {
                            _id "docs-search-input"
                            _type "search"
                            _placeholder "Search pages and headings"
                            _autocomplete "off"
                            _data("on:input", filterScript)
                        }
                        button { _type "button"; _ariaLabel "Close search"; _data("on:click", "document.getElementById('docs-search-dialog').close(); document.getElementById('docs-search-button').focus()"); "Close" }
                    }
                    div {
                        _id "docs-search-results"
                        _class "max-h-[30rem] overflow-y-auto p-2 [&>a]:flex [&>a]:flex-col [&>a]:gap-0.5 [&>a]:rounded-lg [&>a]:px-3 [&>a]:py-2.5 [&>a]:text-[var(--fve-text)] [&>a]:no-underline [&>a:hover]:bg-[var(--fve-surface-hover)] [&>a:focus-visible]:bg-[var(--fve-surface-hover)] [&>a:focus-visible]:outline-none [&_a>span]:text-sm [&_a>span]:leading-snug [&_a>span]:text-[var(--fve-muted-text)]"
                        for result in results do
                            a {
                                _href result.href
                                _data("docs-search-entry", "true")
                                _data("docs-search-text", searchable result)
                                _data("on:click", "document.getElementById('docs-search-dialog').close()")
                                strong { result.title }
                                span { result.description }
                            }
                    }
                }
            }
            script { raw "window.addEventListener('keydown', event => { if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') { event.preventDefault(); document.getElementById('docs-search-dialog')?.showModal(); queueMicrotask(() => document.getElementById('docs-search-input')?.focus()); } });" }
        }
