namespace FSharp.ViewEngine.Docs

open FSharp.ViewEngine
open System
open System.Text.Json
open type Html

/// Consumer-provided search metadata for a documentation page.
type DocsSearchEntry =
    { href:string
      page:DocsPage
      keywords:string list }

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
            _class "docs-search"
            button {
                _id "docs-search-button"
                _type "button"
                _ariaLabel "Search documentation"
                _ariaHaspopup "dialog"
                _class "docs-search-button"
                _data("on:click", "document.getElementById('docs-search-dialog').showModal(); queueMicrotask(() => document.getElementById('docs-search-input').focus())")
                span { "Search" }
                kbd { "Ctrl+K" }
            }
            dialog {
                _id "docs-search-dialog"
                _ariaLabel "Search documentation"
                _class "docs-search-dialog"
                _data("on:click", "evt.target === evt.currentTarget && evt.currentTarget.close()")
                div {
                    _class "docs-search-panel"
                    div {
                        _class "docs-search-field"
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
                        _class "docs-search-results"
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
