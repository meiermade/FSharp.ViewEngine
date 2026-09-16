namespace FSharp.ViewEngine.Components.Documentation

open FSharp.ViewEngine
open System
open type Html

module Example =
    let private render gallery codeFirst (id:string) (label:string) (language:string) (source:string) (preview:HtmlElement) =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "An example ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An example label is required."

        let token =
            id
            |> Seq.map (fun character -> if Char.IsLetterOrDigit character then character else '_')
            |> Seq.toArray
            |> String

        let signal = $"{token}Example"
        let initial = if codeFirst then "code" else "preview"
        let normalized = if String.IsNullOrWhiteSpace language then "text" else language.Trim().ToLowerInvariant()
        let prismLanguage = if normalized = "fs" then "fsharp" else normalized
        let tabId name = $"{id}-tab-{name}"
        let panelId name = $"{id}-panel-{name}"
        let select name = $"${signal} = '{name}'"
        let activate name =
            if name = "code" then $", queueMicrotask(() => window.renderCode?.(document.getElementById('{panelId name}')))"
            else $", queueMicrotask(() => window.renderDocsPreview?.(document.getElementById('{panelId name}')))"
        let focus name = $"{select name}, document.getElementById('{tabId name}').focus(){activate name}"
        let focusPreview = focus "preview"
        let focusCode = focus "code"

        section {
            _class (if gallery then "spec-example spec-example-gallery docs-copyable-code" else "spec-example docs-copyable-code")
            _data("docs-example", "true")
            _data("signals", $"{{ {signal}: '{initial}' }}")
            div {
                _class "spec-example-toolbar"
                if gallery then h2 { _class "spec-example-title"; label }
                else div { _class "spec-example-title"; label }
                div {
                    _role "tablist"
                    _ariaLabel label
                    _class "spec-example-tabs"
                    let tabs = [ "code", "Code", "preview"; "preview", "Preview", "code" ]
                    for name, tabLabel, next in (if gallery then List.rev tabs else tabs) do
                        let isSelected = name = initial
                        let dynamicAction = activate name
                        button {
                            _id (tabId name)
                            _type "button"
                            _role "tab"
                            _ariaControls (panelId name)
                            _ariaSelected isSelected
                            _data("attr:aria-selected", $"${signal} == '{name}' ? 'true' : 'false'")
                            _data("attr:data-selected", $"${signal} == '{name}' ? 'true' : null")
                            _data("attr:tabindex", $"${signal} == '{name}' ? 0 : -1")
                            _data("on:click", $"{select name}{dynamicAction}")
                            _data("on:keydown", $"(evt.key == 'ArrowLeft' || evt.key == 'ArrowRight') && (evt.preventDefault(), {focus next}); evt.key == 'Home' && (evt.preventDefault(), {if gallery then focusPreview else focusCode}); evt.key == 'End' && (evt.preventDefault(), {if gallery then focusCode else focusPreview})")
                            _class "spec-example-tab"
                            tabLabel
                        }
                }
            }
            div {
                _id (panelId "preview")
                _role "tabpanel"
                _attr("aria-labelledby", tabId "preview")
                _class "spec-example-preview"
                _data("show", $"${signal} == 'preview'")
                if codeFirst then _style "display:none"
                else _data("docs-preview-initial", "true")
                preview
            }
            pre {
                _id (panelId "code")
                _role "tabpanel"
                _attr("aria-labelledby", tabId "code")
                _tabindex 0
                _class $"spec-example-code spec-code language-{prismLanguage}"
                _data("show", $"${signal} == 'code'")
                if not codeFirst then _style "display:none"
                button {
                    _type "button"
                    _ariaLabel $"Copy {label} code"
                    _attr("title", "Copy code")
                    _class "docs-copy-code"
                    _data("on:click", "window.fsharpDocsCopy(evt.currentTarget)")
                    raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path d="M5.5 2.75A2.75 2.75 0 0 0 2.75 5.5v7A2.75 2.75 0 0 0 5.5 15.25h1.75a.75.75 0 0 0 0-1.5H5.5c-.69 0-1.25-.56-1.25-1.25v-7c0-.69.56-1.25 1.25-1.25h5c.69 0 1.25.56 1.25 1.25v1.75a.75.75 0 0 0 1.5 0V5.5a2.75 2.75 0 0 0-2.75-2.75h-5Z"/><path d="M9.5 8.25A2.75 2.75 0 0 0 6.75 11v4.5a2.75 2.75 0 0 0 2.75 2.75h5A2.75 2.75 0 0 0 17.25 15.5V11a2.75 2.75 0 0 0-2.75-2.75h-5Zm-1.25 2.75c0-.69.56-1.25 1.25-1.25h5c.69 0 1.25.56 1.25 1.25v4.5c0 .69-.56 1.25-1.25 1.25h-5c-.69 0-1.25-.56-1.25-1.25V11Z"/></svg>"""
                    span {
                        _data("docs-copy-label", "true")
                        _class "sr-only"
                        "Copy"
                    }
                }
                code {
                    _class $"language-{prismLanguage}"
                    _data("docs-copy-source", "true")
                    source
                }
            }
        }

    let previewFirst id label language source preview = render false false id label language source preview
    let codeFirst id label language source preview = render false true id label language source preview
    /// A named, preview-first gallery item with copyable code and an h2 toolbar title.
    let gallery id label language source preview = render true false id label language source preview
