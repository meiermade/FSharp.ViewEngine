namespace FSharp.ViewEngine.Docs

open FSharp.ViewEngine
open type Html

type DocsViewport =
    | Mobile
    | Tablet
    | Desktop

module DocsViewport =
    let value = function Mobile -> "mobile" | Tablet -> "tablet" | Desktop -> "desktop"

[<NoEquality; NoComparison>]
type DocsStory =
    { id:string
      title:string
      language:string
      source:string
      preview:HtmlElement
      viewports:DocsViewport list
      themes:DocsColorMode list
      states:string list }

type DocsVersion =
    { label:string
      href:string }

module StoryView =
    let catalog stories =
        div {
            _class "docs-story-catalog"
            for story in stories do
                section {
                    _data("docs-story", story.id)
                    _data("docs-viewports", story.viewports |> List.map DocsViewport.value |> String.concat " ")
                    _data("docs-themes", story.themes |> List.map DocsColorMode.value |> String.concat " ")
                    _data("docs-states", story.states |> String.concat " ")
                    Example.codeFirst story.id story.title story.language story.source story.preview
                }
        }

    let versionSelector current versions =
        nav {
            _ariaLabel "Documentation version"
            _class "docs-version-selector"
            for version in versions do
                a {
                    _href version.href
                    if version.label = current then _ariaCurrent "page"
                    version.label
                }
        }
