namespace FSharp.ViewEngine.Docs

open FSharp.ViewEngine
open System
open type Html

[<NoEquality; NoComparison>]
type WireframeState =
    { id:string
      label:string
      content:HtmlElement }

module Wireframe =
    let browserFrame (canonicalUrl:string) (content:HtmlElement) =
        div {
            _class "spec-browser-frame"
            _data("browser-frame", "true")
            _data("browser-url", canonicalUrl)
            div {
                _class "spec-browser-toolbar"
                div {
                    _class "spec-browser-dots"
                    span { _class "spec-browser-dot spec-browser-dot-red" }
                    span { _class "spec-browser-dot spec-browser-dot-amber" }
                    span { _class "spec-browser-dot spec-browser-dot-green" }
                }
                div {
                    _class "spec-browser-address"
                    if canonicalUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) then
                        raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M5.75 8V6a4.25 4.25 0 0 1 8.5 0v2h.25A1.5 1.5 0 0 1 16 9.5v6a1.5 1.5 0 0 1-1.5 1.5h-9A1.5 1.5 0 0 1 4 15.5v-6A1.5 1.5 0 0 1 5.5 8h.25Zm7 0V6a2.75 2.75 0 1 0-5.5 0v2h5.5Z" clip-rule="evenodd" /></svg>"""
                    span { canonicalUrl }
                }
            }
            content
        }

    let stateTabs (id:string) (label:string) (states:WireframeState list) =
        match states with
        | [] -> invalidArg (nameof states) "At least one wireframe state is required."
        | first :: _ ->
            let token =
                id
                |> Seq.map (fun character -> if Char.IsLetterOrDigit character then character else '_')
                |> Seq.toArray
                |> String

            let signal = $"{token}State"
            let tabId stateId = $"{id}-tab-{stateId}"
            let panelId stateId = $"{id}-panel-{stateId}"
            let focusState stateId = $"${signal} = '{stateId}', document.getElementById('{tabId stateId}').focus()"
            let firstFocus = focusState first.id
            let lastFocus = states |> List.last |> fun state -> focusState state.id

            div {
                _class "spec-state-tabs"
                _data("signals", $"{{ {signal}: '{first.id}' }}")
                div {
                    _role "tablist"
                    _ariaLabel label
                    _class "spec-state-tab-list"
                    for index, state in List.indexed states do
                        let previous = states[(index + states.Length - 1) % states.Length]
                        let next = states[(index + 1) % states.Length]
                        button {
                            _id (tabId state.id)
                            _type "button"
                            _role "tab"
                            _ariaControls (panelId state.id)
                            _data("attr:aria-selected", $"${signal} == '{state.id}' ? 'true' : 'false'")
                            _data("attr:data-selected", $"${signal} == '{state.id}' ? 'true' : null")
                            _data("attr:tabindex", $"${signal} == '{state.id}' ? 0 : -1")
                            _data("on:click", $"${signal} = '{state.id}'")
                            _data("on:keydown", $"evt.key == 'ArrowLeft' && (evt.preventDefault(), {focusState previous.id}); evt.key == 'ArrowRight' && (evt.preventDefault(), {focusState next.id}); evt.key == 'Home' && (evt.preventDefault(), {firstFocus}); evt.key == 'End' && (evt.preventDefault(), {lastFocus})")
                            _class "spec-state-tab"
                            state.label
                        }
                }
                for index, state in List.indexed states do
                    div {
                        _id (panelId state.id)
                        _role "tabpanel"
                        _attr("aria-labelledby", tabId state.id)
                        _data("show", $"${signal} == '{state.id}'")
                        if index > 0 then _style "display:none"
                        state.content
                    }
            }

module Example =
    let private render codeFirst (id:string) (label:string) (language:string) (source:string) (preview:HtmlElement) =
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
            _class "spec-example"
            _data("docs-example", "true")
            _data("signals", $"{{ {signal}: '{initial}' }}")
            div {
                _class "spec-example-toolbar"
                div { _class "spec-example-title"; label }
                div {
                    _role "tablist"
                    _ariaLabel label
                    _class "spec-example-tabs"
                    for name, tabLabel, next in [ "code", "Code", "preview"; "preview", "Preview", "code" ] do
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
                            _data("on:keydown", $"(evt.key == 'ArrowLeft' || evt.key == 'ArrowRight') && (evt.preventDefault(), {focus next}); evt.key == 'Home' && (evt.preventDefault(), {focusCode}); evt.key == 'End' && (evt.preventDefault(), {focusPreview})")
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
                code { _class $"language-{prismLanguage}"; source }
            }
        }

    let previewFirst id label language source preview = render false id label language source preview
    let codeFirst id label language source preview = render true id label language source preview
